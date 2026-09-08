# 🎫 HỆ THỐNG QUẢN LÝ HỖ TRỢ KỸ THUẬT (HELPDESK SYSTEM)

Dự án xây dựng hệ thống Helpdesk doanh nghiệp hiện đại, đa kênh (Omnichannel), hỗ trợ Multi-tenancy và SLA dựa trên nền tảng **ABP Framework v10.6**, **.NET 10**, **PostgreSQL** và **Angular 22**.

---

## 📑 MỤC LỤC

1. [Tổng Quan Dự Án & Kiến Trúc](#-tổng-quan-dự-án--kiến-trúc)
2. [Lộ Trình Triển Khai (Roadmap 7 Phân Hệ)](#-lộ-trình-triển-khai-roadmap-7-phân-hệ)
3. [Báo Cáo Triển Khai Phân Hệ 1: Master Data](#-báo-cáo-triển-khai-phân-hệ-1-quản-lý-danh-mục-master-data)
   - [3.1. Thiết Kế & Cấu Trúc Thực Thể (Entities)](#31-thiết-kế--cấu-trúc-thực-thể-entities)
   - [3.2. Chi Tiết Các Tầng Kiến Trúc DDD](#32-chi-tiết-các-tầng-kiến-trúc-ddd)
   - [3.3. Ma Trận Phân Quyền (Permissions)](#33-ma-trận-phân-quyền-permissions)
   - [3.4. Dữ Liệu Khởi Tạo Mặc Định (Data Seeder)](#34-dữ-liệu-khởi-tạo-mặc-định-data-seeder)
   - [3.5. Bảng Thống Kê Các File Triển Khai](#35-bảng-thống-kê-các-file-triển-khai)
4. [Hướng Dẫn Vận Hành & Khởi Chạy](#-hướng-dẫn-vận-hành--khởi-chạy)
   - [4.1. Tạo Migration & Cập Nhật Database](#41-tạo-migration--cập-nhật-database)
   - [4.2. Khởi Chạy Backend API (Swagger)](#42-khởi-chạy-backend-api-swagger)
   - [4.3. Hướng Dẫn Angular Frontend](#43-hướng-dẫn-angular-frontend)
5. [Kế Hoạch Tiếp Theo](#-kế-hoạch-tiếp-theo)

---

## 🏛️ TỔNG QUAN DỰ ÁN & KIẾN TRÚC

### 1. Công Nghệ Sử Dụng
- **Backend**: .NET 10.0, C# 13, ABP Framework v10.6.0
- **Database**: PostgreSQL 16+ với Entity Framework Core 10
- **Architecture**: Domain-Driven Design (DDD) phân tầng chuẩn mực
- **Multi-Tenancy**: Sẵn sàng cho mô hình SaaS (`IMultiTenant`)
- **Frontend**: Angular 22, ABP Angular packages, Bootstrap / NgRx

### 2. Cấu Trúc Solution
```
Helpdesk/
├── src/
│   ├── Helpdesk.Domain.Shared/       # Hằng số, Enum, ErrorCodes, Localization
│   ├── Helpdesk.Domain/              # Entities, Domain Managers, Repositories (Interface), Data Seeder
│   ├── Helpdesk.Application.Contracts/ # DTOs, Permissions, Service Interfaces
│   ├── Helpdesk.Application/         # Application Services thực thi nghiệp vụ (CRUD, Mapping, Rules)
│   ├── Helpdesk.EntityFrameworkCore/ # DbContext, Fluent Configurations, EF Core Repositories
│   ├── Helpdesk.HttpApi/             # API Controllers (auto-api)
│   ├── Helpdesk.HttpApi.Host/        # Host API Web App, OpenIddict, Swagger UI
│   └── Helpdesk.DbMigrator/          # Console tool chạy Migrations & Data Seeders
├── angular/                          # Single Page Application (Angular)
└── test/                             # Unit & Integration Tests cho từng tầng
```

---

## 🗺️ LỘ TRÌNH TRIỂN KHAI (ROADMAP 7 PHÂN HỆ)

| STT | Phân Hệ | Mục Tiêu & Chức Năng Chính | Trạng Thái |
|:---:|:---|:---|:---:|
| **1** | **Quản Lý Danh Mục (Master Data)** | Phân loại Category, Priority, Department, Status, Source, Canned Response | **Hoàn thành (Backend)** |
| **2** | **Quản Lý Ticket Cốt Lõi (Ticket Core)** | Vòng đời vé hỗ trợ, chuyển trạng thái, phân công, bình luận, đính kèm tệp | Chuẩn bị triển khai |
| **3** | **Quản Lý Cam Kết Dịch Vụ (SLA Engine)** | Định nghĩa quy tắc SLA, cảnh báo trễ hạn, tính giờ làm việc (Business Hours) | Lập kế hoạch |
| **4** | **Báo Cáo & Thống Kê (Dashboard & Analytics)** | KPI xử lý, phân tích theo nhân viên/phòng ban, biểu đồ xu hướng | Lập kế hoạch |
| **5** | **Hệ Thống Thông Báo (Notifications)** | Real-time SignalR, Email thông báo cập nhật vé, nhắc hạn tự động | Lập kế hoạch |
| **6** | **Cơ Sở Tri Thức (Knowledge Base - FAQ)** | Bài viết hướng dẫn tự phục vụ, giải đáp thắc mắc người dùng | Lập kế hoạch |
| **7** | **Cổng Khách Hàng (Customer Portal)** | Giao diện riêng cho khách hàng theo dõi vé, đánh giá chất lượng dịch vụ | Lập kế hoạch |

---

## 🚀 BÁO CÁO TRIỂN KHAI PHÂN HỆ 1: QUẢN LÝ DANH MỤC (MASTER DATA)

### 3.1. Thiết Kế & Cấu Trúc Thực Thể (Entities)

Toàn bộ 6 thực thể được thiết kế tuân thủ nghiêm ngặt nguyên lý DDD:
- Kế thừa `FullAuditedAggregateRoot<Guid>` (tự động theo dõi `CreationTime`, `CreatorId`, `LastModificationTime`, `IsDeleted`, `DeleterId`...)
- Thực thi interface `IMultiTenant` (phục vụ tách biệt dữ liệu theo TenantId)
- Encapsulation: Dùng private setters, cập nhật thuộc tính thông qua constructor và domain methods với validation kiểm tra null/empty/độ dài.

```
                    ┌─────────────────────────┐
                    │      Department         │
                    │ (Phòng ban chuyên trách)│
                    └───────────┬─────────────┘
                                │ 1:N
┌──────────────────┐   ┌────────▼────────┐   ┌──────────────────┐
│     Priority     │   │    Category     │   │   TicketStatus   │
│ (Mức ưu tiên)    │   │ (Cây phân cấp)  │   │  (Trạng thái vé) │
└──────────────────┘   └────────┬────────┘   └──────────────────┘
                                │ 1:N
┌──────────────────┐   ┌────────▼────────┐
│   TicketSource   │   │ CannedResponse  │
│ (Kênh tiếp nhận) │   │ (Mẫu phản hồi)  │
└──────────────────┘   └─────────────────┘
```

#### 1. Category (Danh mục hỗ trợ)
- **Mục đích**: Phân loại yêu cầu hỗ trợ theo lĩnh vực kỹ thuật/nghiệp vụ.
- **Tính năng đặc biệt**: Hỗ trợ cây phân cấp cha - con qua `ParentId`, cấu hình `SupportEmail` riêng cho từng danh mục.
- **Các trường chính**: `Code`, `Name`, `Description`, `ParentId`, `SortOrder`, `IsActive`, `SupportEmail`.

#### 2. Priority (Mức độ ưu tiên)
- **Mục đích**: Xác định tính cấp bách của sự cố và chuẩn hóa thời hạn phản hồi/xử lý.
- **Tính năng đặc biệt**: Tích hợp sẵn `FirstResponseHours` và `ResolutionHours` (chuẩn bị cho SLA Engine), mã màu `ColorHex` phục vụ giao diện.
- **Các trường chính**: `Code`, `Name`, `Description`, `Level` (int), `ColorHex`, `FirstResponseHours`, `ResolutionHours`, `IsDefault`, `IsActive`.

#### 3. Department (Phòng ban xử lý)
- **Mục đích**: Nhóm kỹ thuật viên và bộ phận chịu trách nhiệm giải quyết yêu cầu.
- **Tính năng đặc biệt**: Liên kết trực tiếp tới người phụ trách `ManagerId` (`IdentityUser`).
- **Các trường chính**: `Code`, `Name`, `Description`, `Email`, `ManagerId`, `IsActive`.

#### 4. TicketStatus (Trạng thái vé)
- **Mục đích**: Kiểm soát từng giai đoạn trong vòng đời xử lý vé.
- **Tính năng đặc biệt**: Phân nhóm bằng Enum `StatusGroup` (`Open`, `InProgress`, `Closed`), đánh dấu cờ `IsDefault` (trạng thái lúc tạo) và `IsFinal` (trạng thái đóng/kết thúc).
- **Các trường chính**: `Code`, `Name`, `Description`, `Group`, `ColorHex`, `SortOrder`, `IsDefault`, `IsFinal`, `IsActive`.

#### 5. TicketSource (Kênh tiếp nhận)
- **Mục đích**: Nhận diện nguồn gốc phát sinh yêu cầu hỗ trợ.
- **Các trường chính**: `Code`, `Name`, `Description`, `Icon`, `IsDefault`, `IsActive`.

#### 6. CannedResponse (Mẫu câu trả lời sẵn)
- **Mục đích**: Tăng tốc độ và tính chuẩn hóa khi hỗ trợ viên phản hồi các câu hỏi lặp lại.
- **Tính năng đặc biệt**: Hỗ trợ phím tắt gợi ý `Shortcut`, liên kết theo `CategoryId` và `DepartmentId`, bộ đếm số lần sử dụng `UsageCount`.
- **Các trường chính**: `Title`, `Content`, `Shortcut`, `CategoryId`, `DepartmentId`, `UsageCount`, `IsActive`.

---

### 3.2. Chi Tiết Các Tầng Kiến Trúc DDD

#### 1. Tầng Domain.Shared
- **Constants**: `CategoryConsts`, `PriorityConsts`, `DepartmentConsts`, `TicketStatusConsts`, `TicketSourceConsts`, `CannedResponseConsts` chuẩn hóa MaxLengths.
- **Enums**: `StatusGroup` (`Open = 1`, `InProgress = 2`, `Closed = 3`).
- **Error Codes**: `HelpdeskDomainErrorCodes` định nghĩa lỗi vi phạm dữ liệu (mã trùng lặp).
- **Localization**: Bổ sung hơn 110 nhãn dịch tiếng Anh trong `en.json` cho toàn bộ thực thể, trường thuộc tính và thông điệp lỗi.

#### 2. Tầng Domain
- **6 Aggregate Roots**: Cài đặt logic nghiệp vụ cốt lõi, bảo vệ tính toàn vẹn dữ liệu.
- **5 Domain Managers**: `CategoryManager`, `PriorityManager`, `DepartmentManager`, `TicketStatusManager`, `TicketSourceManager` chịu trách nhiệm validate tính duy nhất của `Code` trong cùng Tenant trước khi thêm mới hoặc cập nhật.
- **6 Custom Repository Interfaces**: Khai báo các phương thức nghiệp vụ chuyên biệt (`FindByCodeAsync`, `GetListWithDetailsAsync`...).
- **Data Seeder**: `HelpdeskDataSeedContributor` tự động nạp dữ liệu mẫu ban đầu khi khởi tạo hệ thống.

#### 3. Tầng Application.Contracts
- **DTOs**: Thiết kế tách biệt `*Dto`, `CreateUpdate*Dto`, `*GetListInput` (hỗ trợ phân trang, sắp xếp và lọc theo từ khóa, trạng thái active).
- **Lookup DTOs**: `CategoryLookupDto` phục vụ dropdown cha-con.
- **Interfaces**: Kế thừa `ICrudAppService<TEntityDto, Guid, TGetListInput, TCreateUpdateDto>`.
- **Permissions**: Khai báo cấu trúc quyền 4 cấp (Default, Create, Update, Delete) cho cả 6 danh mục.

#### 4. Tầng Application
- **6 Application Services**: Kế thừa `CrudAppService`, tích hợp sẵn Authorization Policies cho từng hành động.
- Áp dụng Domain Manager trong pipeline `CreateAsync` và `UpdateAsync`.
- Tối ưu hóa truy vấn `CreateFilteredQueryAsync` hỗ trợ tìm kiếm đa trường và lọc trạng thái linh hoạt.

#### 5. Tầng EntityFrameworkCore
- **DbContext & Fluent API**: Cấu hình bảng dữ liệu qua `HelpdeskDbContextModelCreatingExtensions` với tiền tố bảng `App` (ví dụ: `AppCategories`, `AppPriorities`...).
- **Database Indexes**: Tạo Composite Index trên `[TenantId, Code]` (Unique Index) và `[TenantId, IsActive]` để tối ưu hóa hiệu năng truy vấn.
- **6 EF Core Repositories**: Thực thi đầy đủ các repository interfaces tùy biến.

---

### 3.3. Ma Trận Phân Quyền (Permissions)

Tổng cộng **24 permissions** được thiết lập trong `HelpdeskPermissions.cs` và đăng ký vào `HelpdeskPermissionDefinitionProvider.cs`:

| Nhóm Danh Mục | Xem (Default) | Thêm mới (Create) | Sửa (Edit) | Xóa (Delete) |
|:---|:---:|:---:|:---:|:---:|
| **Categories** | `Helpdesk.Categories` | `Helpdesk.Categories.Create` | `Helpdesk.Categories.Edit` | `Helpdesk.Categories.Delete` |
| **Priorities** | `Helpdesk.Priorities` | `Helpdesk.Priorities.Create` | `Helpdesk.Priorities.Edit` | `Helpdesk.Priorities.Delete` |
| **Departments** | `Helpdesk.Departments` | `Helpdesk.Departments.Create` | `Helpdesk.Departments.Edit` | `Helpdesk.Departments.Delete` |
| **TicketStatuses** | `Helpdesk.TicketStatuses` | `Helpdesk.TicketStatuses.Create` | `Helpdesk.TicketStatuses.Edit` | `Helpdesk.TicketStatuses.Delete` |
| **TicketSources** | `Helpdesk.TicketSources` | `Helpdesk.TicketSources.Create` | `Helpdesk.TicketSources.Edit` | `Helpdesk.TicketSources.Delete` |
| **CannedResponses** | `Helpdesk.CannedResponses` | `Helpdesk.CannedResponses.Create` | `Helpdesk.CannedResponses.Edit` | `Helpdesk.CannedResponses.Delete` |

---

### 3.4. Dữ Liệu Khởi Tạo Mặc Định (Data Seeder)

Khi chạy `Helpdesk.DbMigrator`, `HelpdeskDataSeedContributor` sẽ tự động khởi tạo bộ dữ liệu tiêu chuẩn:

1. **Priorities (4 mức)**:
   - `LOW`: Thấp (Màu xanh lam `#28A745`, Phản hồi 24h, Xử lý 72h)
   - `MEDIUM`: Trung bình - Mặc định (Màu cam `#FFC107`, Phản hồi 8h, Xử lý 24h)
   - `HIGH`: Cao (Màu cam đỏ `#FD7E14`, Phản hồi 4h, Xử lý 8h)
   - `URGENT`: Khẩn cấp (Màu đỏ `#DC3545`, Phản hồi 1h, Xử lý 4h)

2. **TicketStatuses (7 trạng thái)**:
   - Nhóm `Open`: `NEW` (Mới tiếp nhận - Default), `OPEN` (Đang mở)
   - Nhóm `InProgress`: `IN_PROGRESS` (Đang xử lý), `PENDING_CUSTOMER` (Chờ khách phản hồi), `PENDING_VENDOR` (Chờ đối tác)
   - Nhóm `Closed`: `RESOLVED` (Đã giải quyết), `CLOSED` (Đã đóng - IsFinal)

3. **TicketSources (5 kênh)**:
   - `PORTAL`: Cổng hỗ trợ trực tuyến (Default)
   - `EMAIL`: Hộp thư hỗ trợ
   - `PHONE`: Đường dây nóng
   - `CHAT`: Trực tuyến (Livechat)
   - `IN_PERSON`: Tiếp nhận trực tiếp

4. **Departments (2 bộ phận mẫu)**:
   - `IT_SUPPORT`: Bộ phận Hỗ trợ CNTT
   - `CUSTOMER_SERVICE`: Bộ phận Chăm sóc khách hàng

5. **Categories (5 danh mục mẫu)**:
   - `HARDWARE`: Sự cố Phần cứng
   - `SOFTWARE`: Sự cố Phần mềm
   - `NETWORK`: Mạng & Kết nối
   - `ACCOUNT`: Tài khoản & Quyền truy cập
   - `GENERAL`: Yêu cầu chung

---

### 3.5. Bảng Thống Kê Các File Triển Khai

| Tầng Dự Án | Số file mới | Số file sửa | Chi tiết chính |
|:---|:---:|:---:|:---|
| **Domain.Shared** | 7 | 2 | Constants, Enum, Error Codes, Localization (`en.json`) |
| **Domain** | 18 | 0 | 6 Entities, 6 Repositories, 5 Domain Managers, Data Seeder |
| **Application.Contracts** | 20 | 2 | 6 DTOs, 6 Inputs, 6 Interfaces, Permissions |
| **Application** | 6 | 0 | 6 Application Services (CRUD, validation, mapping) |
| **EntityFrameworkCore** | 7 | 1 | DbContext, ModelCreatingExtensions, 6 EF Repositories |
| **TỔNG CỘNG** | **58 files mới** | **5 files sửa** | **Build thành công 100% (0 errors)** |

---

## 🛠️ HƯỚNG DẪN VẬN HÀNH & KHỞI CHẠY

### 4.1. Tạo Migration & Cập Nhật Database

> **Yêu cầu**: Đảm bảo PostgreSQL đã chạy (mặc định tại `localhost:5432`, database: `appdb`). Cấu hình chuỗi kết nối trong `appsettings.json`.

Mở terminal tại thư mục gốc `d:\helpdesk\Helpdesk`:

**Bước 1: Tạo EF Core Migration cho 6 thực thể mới**
```powershell
dotnet ef migrations add Added_MasterData_Entities -p src/Helpdesk.EntityFrameworkCore -s src/Helpdesk.HttpApi.Host
```

**Bước 2: Cập nhật CSDL và tự động nạp dữ liệu mẫu (Seeder)**
```powershell
dotnet run --project src/Helpdesk.DbMigrator
```
*Lệnh này sẽ tự động tạo các bảng `AppCategories`, `AppPriorities`, `AppDepartments`... và nạp toàn bộ danh mục mẫu đã định cấu hình.*

---

### 4.2. Khởi Chạy Backend API (Swagger)

Khởi động dự án Web API Host:
```powershell
dotnet run --project src/Helpdesk.HttpApi.Host
```
- **URL Swagger UI**: `https://localhost:44346/swagger`
- Tại đây, bạn sẽ thấy đầy đủ danh sách RESTful APIs cho:
  - `/api/app/category`
  - `/api/app/priority`
  - `/api/app/department`
  - `/api/app/ticket-status`
  - `/api/app/ticket-source`
  - `/api/app/canned-response`

---

### 4.3. Hướng Dẫn Angular Frontend

Khi sẵn sàng xây dựng màn hình CRUD trên giao diện Angular:

1. **Sinh Proxy API cho Angular Client**:
   ```bash
   cd angular
   abp generate-proxy -t ng
   ```
2. Thêm module hoặc routing cho các trang quản trị danh mục (Master Data Management).

---

## 📌 KẾ HOẠCH TIẾP THEO

- [ ] **Giai đoạn 1.B**: Phát triển giao diện Angular (Data Table, Form Modal, Filter, Tree View cho Category) cho Phân hệ Master Data.
- [ ] **Giai đoạn 2**: Triển khai **Phân hệ 2 - Ticket Core (Xử lý phiếu hỗ trợ)**:
  - Aggregate Root `Ticket` (Mã vé tự sinh, Tiêu đề, Mô tả, Khách hàng, Trạng thái, Phân công)
  - `TicketComment` (Bình luận nội bộ & phản hồi khách hàng)
  - `TicketAttachment` (Tệp đính kèm)
  - `TicketActivityLog` (Lịch sử thao tác / Audit Trail)
- [ ] **Giai đoạn 3**: Triển khai **Phân hệ 3 - SLA Management Engine**.
