using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

#nullable disable

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261103100000_AddNormalizedCategoryNames")]
public partial class AddNormalizedCategoryNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            schema: "products",
            table: "Categories",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE products.\"Categories\" SET \"NormalizedName\" = UPPER(BTRIM(\"Name\"));");

        migrationBuilder.AlterColumn<string>(
            name: "NormalizedName",
            schema: "products",
            table: "Categories",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.DropIndex(
            name: "IX_Categories_Name",
            schema: "products",
            table: "Categories");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_NormalizedName",
            schema: "products",
            table: "Categories",
            column: "NormalizedName",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Categories_NormalizedName",
            schema: "products",
            table: "Categories");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            schema: "products",
            table: "Categories");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            schema: "products",
            table: "Categories",
            column: "Name",
            unique: true);
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        new SnapshotModel().Build(modelBuilder);
    }

    private sealed class SnapshotModel : ProductDbContextModelSnapshot
    {
        public void Build(ModelBuilder modelBuilder) => BuildModel(modelBuilder);
    }
}
