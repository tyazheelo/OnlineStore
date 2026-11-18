using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

#nullable disable

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261027100000_AddProductReviews")]
public partial class AddProductReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductReviews",
            schema: "products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ReviewerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Rating = table.Column<int>(type: "integer", nullable: false),
                Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductReviews", review => review.Id);
                table.CheckConstraint("CK_ProductReviews_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5");
                table.ForeignKey(
                    name: "FK_ProductReviews_Products_ProductId",
                    column: review => review.ProductId,
                    principalSchema: "products",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductReviews_ProductId_CreatedAtUtc",
            schema: "products",
            table: "ProductReviews",
            columns: new[] { "ProductId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ProductReviews_ProductId_UserId",
            schema: "products",
            table: "ProductReviews",
            columns: new[] { "ProductId", "UserId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductReviews", schema: "products");
    }
}
