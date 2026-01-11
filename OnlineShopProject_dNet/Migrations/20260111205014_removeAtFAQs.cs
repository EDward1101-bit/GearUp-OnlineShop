using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShopProject_dNet.Migrations
{
    /// <inheritdoc />
    public partial class removeAtFAQs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpfulCount",
                table: "FAQs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HelpfulCount",
                table: "FAQs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
