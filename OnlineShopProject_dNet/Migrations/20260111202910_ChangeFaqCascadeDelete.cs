using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShopProject_dNet.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFaqCascadeDelete : Migration
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
                onDelete: ReferentialAction.Cascade);
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
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
