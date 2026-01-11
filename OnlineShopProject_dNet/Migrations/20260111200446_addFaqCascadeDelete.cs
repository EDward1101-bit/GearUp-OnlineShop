using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShopProject_dNet.Migrations
{
    /// <inheritdoc />
    public partial class addFaqCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FAQs_Products_ProductId",
                table: "FAQs");

            migrationBuilder.AddForeignKey(
                name: "FK_FAQs_Products_ProductId",
                table: "FAQs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FAQs_Products_ProductId",
                table: "FAQs");

            migrationBuilder.AddForeignKey(
                name: "FK_FAQs_Products_ProductId",
                table: "FAQs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
