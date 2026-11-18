using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Data;

#nullable disable

namespace Order.Infrastructure.Data.Migrations;

[DbContext(typeof(OrderDbContext))]
[Migration("20261111113500_AddInventoryConfirmationOutbox")]
public sealed class AddInventoryConfirmationOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Orders_CreatedAt",
            schema: "orders",
            table: "Orders",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_CreatedAt",
            schema: "orders",
            table: "Orders",
            columns: new[] { "Status", "CreatedAt" });

        migrationBuilder.CreateTable(
            name: "InventoryConfirmationOutbox",
            schema: "orders",
            columns: table => new
            {
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryConfirmationOutbox", item => item.OrderId);
                table.ForeignKey(
                    name: "FK_InventoryConfirmationOutbox_Orders_OrderId",
                    column: item => item.OrderId,
                    principalSchema: "orders",
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryConfirmationOutbox_NextAttemptAtUtc",
            schema: "orders",
            table: "InventoryConfirmationOutbox",
            column: "NextAttemptAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InventoryConfirmationOutbox",
            schema: "orders");

        migrationBuilder.DropIndex(
            name: "IX_Orders_CreatedAt",
            schema: "orders",
            table: "Orders");

        migrationBuilder.DropIndex(
            name: "IX_Orders_Status_CreatedAt",
            schema: "orders",
            table: "Orders");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        new SnapshotModel().Build(modelBuilder);
    }

    private sealed class SnapshotModel : OrderDbContextModelSnapshot
    {
        public void Build(ModelBuilder modelBuilder) => BuildModel(modelBuilder);
    }
}
