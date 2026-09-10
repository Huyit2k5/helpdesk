using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class Add_Discord_Thread_And_Comment_Author : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordThreadId",
                table: "AppTickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "AppTicketComments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTickets_DiscordThreadId",
                table: "AppTickets",
                column: "DiscordThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTickets_DiscordThreadId",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "DiscordThreadId",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "AppTicketComments");
        }
    }
}
