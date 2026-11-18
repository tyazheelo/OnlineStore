using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Product.Infrastructure.Migrations;

public partial class AddProductCategories : Migration
{
    private static readonly Guid UncategorizedCategoryId =
        new("8a173dd5-72c2-4e0d-9e1e-68d3a3160004");

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "products");

        migrationBuilder.CreateTable(
            name: "Categories",
            schema: "products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Categories", category => category.Id));

        migrationBuilder.InsertData(
            schema: "products",
            table: "Categories",
            columns: new[] { "Id", "Name" },
            values: new object[,]
            {
                { new Guid("8a173dd5-72c2-4e0d-9e1e-68d3a3160001"), "Аксессуары" },
                { new Guid("8a173dd5-72c2-4e0d-9e1e-68d3a3160002"), "Для дома" },
                { new Guid("8a173dd5-72c2-4e0d-9e1e-68d3a3160003"), "Техника" },
                { UncategorizedCategoryId, "Без категории" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            schema: "products",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CategoryId",
            schema: "products",
            table: "Products",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            $"UPDATE products.\"Products\" SET \"CategoryId\" = '{UncategorizedCategoryId}' WHERE \"CategoryId\" IS NULL;");

        migrationBuilder.AlterColumn<Guid>(
            name: "CategoryId",
            schema: "products",
            table: "Products",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            schema: "products",
            table: "Products",
            column: "CategoryId");

        migrationBuilder.AddForeignKey(
            name: "FK_Products_Categories_CategoryId",
            schema: "products",
            table: "Products",
            column: "CategoryId",
            principalSchema: "products",
            principalTable: "Categories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Products_Categories_CategoryId",
            schema: "products",
            table: "Products");

        migrationBuilder.DropIndex(
            name: "IX_Products_CategoryId",
            schema: "products",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "CategoryId",
            schema: "products",
            table: "Products");

        migrationBuilder.DropTable(
            name: "Categories",
            schema: "products");
    }
}
