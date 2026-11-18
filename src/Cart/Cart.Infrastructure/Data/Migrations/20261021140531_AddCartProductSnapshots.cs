using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCartProductSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "cart",
                table: "CartItems",
                newName: "UnitPriceSnapshot");

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                schema: "cart",
                table: "CartItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                schema: "cart",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "UnitPriceSnapshot",
                schema: "cart",
                table: "CartItems",
                newName: "Price");
        }
    }
}
