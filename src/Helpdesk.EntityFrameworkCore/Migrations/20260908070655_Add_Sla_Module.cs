using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class Add_Sla_Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstRespondedAt",
                table: "AppTickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseDueDate",
                table: "AppTickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstResponseBreached",
                table: "AppTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolutionBreached",
                table: "AppTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SlaPolicyId",
                table: "AppTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppBusinessHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsWorkDay = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBusinessHours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSlaBreachLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlaPolicyRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreachType = table.Column<int>(type: "integer", nullable: false),
                    BreachedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpectedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResolvedOrRespondedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ElapsedMinutes = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSlaBreachLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSlaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSlaPolicyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlaPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    ResolutionTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    EscalationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EscalateToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EscalateToDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSlaPolicyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSlaPolicyRules_AppSlaPolicies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalTable: "AppSlaPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBusinessHours_DayOfWeek",
                table: "AppBusinessHours",
                column: "DayOfWeek");

            migrationBuilder.CreateIndex(
                name: "IX_AppHolidays_Date",
                table: "AppHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AppSlaBreachLogs_BreachedAt",
                table: "AppSlaBreachLogs",
                column: "BreachedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppSlaBreachLogs_TicketId",
                table: "AppSlaBreachLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSlaPolicyRules_SlaPolicyId_PriorityId_CategoryId",
                table: "AppSlaPolicyRules",
                columns: new[] { "SlaPolicyId", "PriorityId", "CategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBusinessHours");

            migrationBuilder.DropTable(
                name: "AppHolidays");

            migrationBuilder.DropTable(
                name: "AppSlaBreachLogs");

            migrationBuilder.DropTable(
                name: "AppSlaPolicyRules");

            migrationBuilder.DropTable(
                name: "AppSlaPolicies");

            migrationBuilder.DropColumn(
                name: "FirstRespondedAt",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseDueDate",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "IsFirstResponseBreached",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "IsResolutionBreached",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "SlaPolicyId",
                table: "AppTickets");
        }
    }
}
