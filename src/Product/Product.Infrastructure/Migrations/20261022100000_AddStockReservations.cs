using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261022100000_AddStockReservations")]
public sealed class AddStockReservations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StockReservations",
            schema: "products",
            columns: table => new
            {
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_StockReservations", reservation => reservation.OrderId));

        migrationBuilder.CreateTable(
            name: "StockReservationItems",
            schema: "products",
            columns: table => new
            {
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockReservationItems", item => new { item.OrderId, item.ProductId });
                table.ForeignKey(
                    name: "FK_StockReservationItems_StockReservations_OrderId",
                    column: item => item.OrderId,
                    principalSchema: "products",
                    principalTable: "StockReservations",
                    principalColumn: "OrderId",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StockReservationItems", schema: "products");
        migrationBuilder.DropTable(name: "StockReservations", schema: "products");
    }
}
