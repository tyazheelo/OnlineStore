using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Cart.Infrastructure.Data;

#nullable disable

namespace Cart.Infrastructure.Data.Migrations;

[DbContext(typeof(CartDbContext))]
[Migration("20261026150000_AddCartRevision")]
public sealed class AddCartRevision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "Revision",
            schema: "cart",
            table: "Carts",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Revision",
            schema: "cart",
            table: "Carts");
    }
}
