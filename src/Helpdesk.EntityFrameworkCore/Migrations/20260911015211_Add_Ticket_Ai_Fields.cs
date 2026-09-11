using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class Add_Ticket_Ai_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiSentiment",
                table: "AppTickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSentimentReason",
                table: "AppTickets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "AppTickets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiSentiment",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "AiSentimentReason",
                table: "AppTickets");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "AppTickets");
        }
    }
}
