using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

#nullable disable

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261027151000_RemoveDemoProducts")]
public partial class RemoveDemoProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM products."Products"
            WHERE "Id" IN (
                'a1100000-0000-4000-8000-000000000001',
                'a1100000-0000-4000-8000-000000000002',
                'a1100000-0000-4000-8000-000000000003',
                'a1100000-0000-4000-8000-000000000004',
                'a1100000-0000-4000-8000-000000000005',
                'a1100000-0000-4000-8000-000000000006'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
