using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShopProject_dNet.Migrations
{
    /// <inheritdoc />
    public partial class Leones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedbackMessage",
                table: "Notifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Notifications",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackMessage",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Notifications");
        }
    }
}
