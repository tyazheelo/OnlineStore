using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.Data;

#nullable disable

namespace Order.Infrastructure.Data.Migrations;

[DbContext(typeof(OrderDbContext))]
[Migration("20261105110000_AddSupportTickets")]
public sealed class AddSupportTickets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "orders");
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS orders."Orders" (
                "Id" uuid NOT NULL CONSTRAINT "PK_Orders" PRIMARY KEY,
                "CustomerId" uuid NOT NULL,
                "OrderNumber" character varying(50) NOT NULL,
                "Country" text NOT NULL,
                "State" text NOT NULL,
                "City" text NOT NULL,
                "Street" text NOT NULL,
                "PostalCode" text NOT NULL,
                "Status" text NOT NULL,
                "DeliveryCost" numeric NOT NULL,
                "PaymentType" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL
            );

            CREATE TABLE IF NOT EXISTS orders."OrderItems" (
                "Id" uuid NOT NULL CONSTRAINT "PK_OrderItems" PRIMARY KEY,
                "OrderId" uuid NOT NULL,
                "ProductId" uuid NOT NULL,
                "ProductName" character varying(200) NOT NULL,
                "Quantity" integer NOT NULL,
                "Price" numeric(18,2) NOT NULL,
                CONSTRAINT "FK_OrderItems_Orders_OrderId"
                    FOREIGN KEY ("OrderId") REFERENCES orders."Orders" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId" ON orders."OrderItems" ("OrderId");
            """);

        migrationBuilder.CreateTable(
            name: "SupportTickets",
            schema: "orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                AssignedAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupportTickets", ticket => ticket.Id);
                table.ForeignKey(
                    name: "FK_SupportTickets_Orders_OrderId",
                    column: ticket => ticket.OrderId,
                    principalSchema: "orders",
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupportMessages",
            schema: "orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupportMessages", message => message.Id);
                table.ForeignKey(
                    name: "FK_SupportMessages_SupportTickets_TicketId",
                    column: message => message.TicketId,
                    principalSchema: "orders",
                    principalTable: "SupportTickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SupportMessages_TicketId_CreatedAt",
            schema: "orders",
            table: "SupportMessages",
            columns: new[] { "TicketId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_SupportTickets_CustomerId",
            schema: "orders",
            table: "SupportTickets",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportTickets_OrderId",
            schema: "orders",
            table: "SupportTickets",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_SupportTickets_Status_CreatedAt",
            schema: "orders",
            table: "SupportTickets",
            columns: new[] { "Status", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SupportMessages", schema: "orders");
        migrationBuilder.DropTable(name: "SupportTickets", schema: "orders");
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
