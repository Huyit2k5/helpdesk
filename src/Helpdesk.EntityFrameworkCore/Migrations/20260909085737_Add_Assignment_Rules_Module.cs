using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class Add_Assignment_Rules_Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppAssignmentRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RoutingStrategy = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriorityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DirectAssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastAssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppAssignmentRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAssignmentRuleAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAssignmentRuleAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAssignmentRuleAgents_AppAssignmentRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "AppAssignmentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignmentRuleAgents_RuleId_UserId",
                table: "AppAssignmentRuleAgents",
                columns: new[] { "RuleId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignmentRules_CategoryId",
                table: "AppAssignmentRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignmentRules_DepartmentId",
                table: "AppAssignmentRules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignmentRules_IsActive",
                table: "AppAssignmentRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignmentRules_Order",
                table: "AppAssignmentRules",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignmentRules_PriorityId",
                table: "AppAssignmentRules",
                column: "PriorityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAssignmentRuleAgents");

            migrationBuilder.DropTable(
                name: "AppAssignmentRules");
        }
    }
}
