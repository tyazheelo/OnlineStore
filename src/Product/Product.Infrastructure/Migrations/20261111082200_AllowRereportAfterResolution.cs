using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

#nullable disable

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261111082200_AllowRereportAfterResolution")]
public partial class AllowRereportAfterResolution : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ProductReviewReports_ReviewId_ReporterUserId",
            schema: "products",
            table: "ProductReviewReports");

        migrationBuilder.CreateIndex(
            name: "IX_ProductReviewReports_ReviewId_ReporterUserId",
            schema: "products",
            table: "ProductReviewReports",
            columns: new[] { "ReviewId", "ReporterUserId" },
            unique: true,
            filter: "\"Status\" = 'Pending'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ProductReviewReports_ReviewId_ReporterUserId",
            schema: "products",
            table: "ProductReviewReports");

        migrationBuilder.CreateIndex(
            name: "IX_ProductReviewReports_ReviewId_ReporterUserId",
            schema: "products",
            table: "ProductReviewReports",
            columns: new[] { "ReviewId", "ReporterUserId" },
            unique: true);
    }
}
