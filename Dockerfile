# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution configurations
COPY common.props NuGet.Config ./

# Copy csproj files for cached restore
COPY src/Helpdesk.Domain.Shared/Helpdesk.Domain.Shared.csproj src/Helpdesk.Domain.Shared/
COPY src/Helpdesk.Domain/Helpdesk.Domain.csproj src/Helpdesk.Domain/
COPY src/Helpdesk.Application.Contracts/Helpdesk.Application.Contracts.csproj src/Helpdesk.Application.Contracts/
COPY src/Helpdesk.Application/Helpdesk.Application.csproj src/Helpdesk.Application/
COPY src/Helpdesk.EntityFrameworkCore/Helpdesk.EntityFrameworkCore.csproj src/Helpdesk.EntityFrameworkCore/
COPY src/Helpdesk.HttpApi/Helpdesk.HttpApi.csproj src/Helpdesk.HttpApi/
COPY src/Helpdesk.HttpApi.Client/Helpdesk.HttpApi.Client.csproj src/Helpdesk.HttpApi.Client/
COPY src/Helpdesk.HttpApi.Host/Helpdesk.HttpApi.Host.csproj src/Helpdesk.HttpApi.Host/

# Restore dependencies
RUN dotnet restore src/Helpdesk.HttpApi.Host/Helpdesk.HttpApi.Host.csproj

# Copy all source code
COPY src/ src/

# Build and publish HttpApi.Host
WORKDIR /src/src/Helpdesk.HttpApi.Host
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Generate openiddict certificate for production token signing
RUN dotnet dev-certs https -v -ep /app/publish/openiddict.pfx -p 4c59229d-1a39-479f-ad26-4c43552ab38a && chmod 644 /app/publish/openiddict.pfx

# Stage 2: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Helpdesk.HttpApi.Host.dll"]
