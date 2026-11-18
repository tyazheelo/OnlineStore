using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

#nullable disable

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261103120000_AddProductEnglishTranslations")]
public partial class AddProductEnglishTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NameEn",
            schema: "products",
            table: "Products",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DescriptionEn",
            schema: "products",
            table: "Products",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpecificationsEn",
            schema: "products",
            table: "Products",
            type: "jsonb",
            nullable: false,
            defaultValueSql: "'{}'::jsonb");

        migrationBuilder.AddColumn<string>(
            name: "NameEn",
            schema: "products",
            table: "Categories",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE products."Products"
            SET "NameEn" = '', "DescriptionEn" = '';

            UPDATE products."Categories"
            SET "NameEn" = CASE "Name"
                WHEN 'Аксессуары' THEN 'Accessories'
                WHEN 'Для дома' THEN 'Home'
                WHEN 'Техника' THEN 'Electronics'
                WHEN 'Без категории' THEN 'Uncategorized'
                ELSE ''
            END;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "NameEn",
            schema: "products",
            table: "Products",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "DescriptionEn",
            schema: "products",
            table: "Products",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(1000)",
            oldMaxLength: 1000,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "NameEn",
            schema: "products",
            table: "Categories",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "NameEn", schema: "products", table: "Products");
        migrationBuilder.DropColumn(name: "DescriptionEn", schema: "products", table: "Products");
        migrationBuilder.DropColumn(name: "SpecificationsEn", schema: "products", table: "Products");
        migrationBuilder.DropColumn(name: "NameEn", schema: "products", table: "Categories");
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
