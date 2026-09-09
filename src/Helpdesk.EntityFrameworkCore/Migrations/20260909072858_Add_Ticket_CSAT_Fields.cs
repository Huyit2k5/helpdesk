using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class Add_Ticket_CSAT_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CsatComment",
                table: "AppTickets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CsatRating",
                table: "AppTickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CsatSubmittedAt",
                table: "AppTickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTickets_CsatRating",
                table: "AppTickets",
                column: "CsatRating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTickets_CsatRating",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "CsatComment",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "CsatRating",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "CsatSubmittedAt",
                table: "AppTickets");
        }
    }
}
