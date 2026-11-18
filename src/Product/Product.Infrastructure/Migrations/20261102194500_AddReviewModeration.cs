using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Product.Infrastructure.Data;

#nullable disable

namespace Product.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261102194500_AddReviewModeration")]
public partial class AddReviewModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsHidden",
            schema: "products",
            table: "ProductReviews",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "ProductReviewReports",
            schema: "products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Reasons = table.Column<string>(type: "jsonb", nullable: false),
                OtherDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModeratorUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductReviewReports", report => report.Id);
                table.ForeignKey(
                    name: "FK_ProductReviewReports_ProductReviews_ReviewId",
                    column: report => report.ReviewId,
                    principalSchema: "products",
                    principalTable: "ProductReviews",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductReviewReports_ReviewId_ReporterUserId",
            schema: "products",
            table: "ProductReviewReports",
            columns: new[] { "ReviewId", "ReporterUserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProductReviewReports_Status_CreatedAtUtc",
            schema: "products",
            table: "ProductReviewReports",
            columns: new[] { "Status", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductReviewReports", schema: "products");
        migrationBuilder.DropColumn(name: "IsHidden", schema: "products", table: "ProductReviews");
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
