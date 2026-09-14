using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class Add_Asset_Management_Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "AppTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetTag = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Location = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToUserName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AssignedToUserEmail = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Specifications = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAssetActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedByUserName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RelatedTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAssetActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAssetActivities_AppAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "AppAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTickets_AssetId",
                table: "AppTickets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssetActivities_ActivityType",
                table: "AppAssetActivities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssetActivities_AssetId",
                table: "AppAssetActivities",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssetActivities_CreationTime",
                table: "AppAssetActivities",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssetActivities_RelatedTicketId",
                table: "AppAssetActivities",
                column: "RelatedTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_AssetTag",
                table: "AppAssets",
                column: "AssetTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_AssetType",
                table: "AppAssets",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_AssignedToUserId",
                table: "AppAssets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_Department",
                table: "AppAssets",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_SerialNumber",
                table: "AppAssets",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_Status",
                table: "AppAssets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssets_WarrantyExpiryDate",
                table: "AppAssets",
                column: "WarrantyExpiryDate");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTickets_AppAssets_AssetId",
                table: "AppTickets",
                column: "AssetId",
                principalTable: "AppAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTickets_AppAssets_AssetId",
                table: "AppTickets");

            migrationBuilder.DropTable(
                name: "AppAssetActivities");

            migrationBuilder.DropTable(
                name: "AppAssets");

            migrationBuilder.DropIndex(
                name: "IX_AppTickets_AssetId",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "AppTickets");
        }
    }
}
