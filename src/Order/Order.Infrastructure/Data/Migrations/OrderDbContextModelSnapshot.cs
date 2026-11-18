using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrderEntity = Order.Domain.Entities.Order;
using OrderItem = Order.Domain.Entities.OrderItem;
using InventoryConfirmationOutbox = Order.Domain.Entities.InventoryConfirmationOutbox;
using SupportMessage = Order.Domain.Entities.SupportMessage;
using SupportTicket = Order.Domain.Entities.SupportTicket;

#nullable disable

namespace Order.Infrastructure.Data.Migrations;

[DbContext(typeof(OrderDbContext))]
public class OrderDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("orders")
            .HasAnnotation("ProductVersion", "9.0.20")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.Property(order => order.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            entity.Property(order => order.CustomerId).HasColumnType("uuid");
            entity.Property(order => order.OrderNumber).IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            entity.Property(order => order.Status).HasConversion<string>().HasColumnType("text");
            entity.Property(order => order.DeliveryCost).HasColumnType("numeric");
            entity.Property(order => order.PaymentType).HasConversion<string>().HasColumnType("text");
            entity.Property(order => order.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(order => order.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.HasKey(order => order.Id);
            entity.HasIndex(order => order.CreatedAt);
            entity.HasIndex(order => new { order.Status, order.CreatedAt });
            entity.ToTable("Orders", "orders");

            entity.OwnsOne(order => order.DeliveryAddress, address =>
            {
                address.Property<Guid>("OrderId").HasColumnType("uuid");
                address.Property(value => value.Country).IsRequired().HasColumnType("text").HasColumnName("Country");
                address.Property(value => value.State).IsRequired().HasColumnType("text").HasColumnName("State");
                address.Property(value => value.City).IsRequired().HasColumnType("text").HasColumnName("City");
                address.Property(value => value.Street).IsRequired().HasColumnType("text").HasColumnName("Street");
                address.Property(value => value.PostalCode).IsRequired().HasColumnType("text").HasColumnName("PostalCode");
                address.HasKey("OrderId");
                address.ToTable("Orders", "orders");
                address.WithOwner().HasForeignKey("OrderId");
            });
            entity.Navigation(order => order.DeliveryAddress).IsRequired();
            entity.HasMany(order => order.Items)
                .WithOne()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            entity.Property(item => item.OrderId).HasColumnType("uuid");
            entity.Property(item => item.ProductId).HasColumnType("uuid");
            entity.Property(item => item.ProductName).IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            entity.Property(item => item.Quantity).HasColumnType("integer");
            entity.Property(item => item.Price).HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.OrderId);
            entity.ToTable("OrderItems", "orders");
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.Property(ticket => ticket.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            entity.Property(ticket => ticket.OrderId).HasColumnType("uuid");
            entity.Property(ticket => ticket.OrderNumber).IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            entity.Property(ticket => ticket.CustomerId).HasColumnType("uuid");
            entity.Property(ticket => ticket.CustomerName).IsRequired().HasMaxLength(250).HasColumnType("character varying(250)");
            entity.Property(ticket => ticket.AssignedAdminId).HasColumnType("uuid");
            entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(20).HasColumnType("character varying(20)");
            entity.Property(ticket => ticket.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(ticket => ticket.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.HasKey(ticket => ticket.Id);
            entity.HasIndex(ticket => ticket.CustomerId);
            entity.HasIndex(ticket => ticket.OrderId);
            entity.HasIndex(ticket => new { ticket.Status, ticket.CreatedAt });
            entity.ToTable("SupportTickets", "orders");
            entity.HasOne<OrderEntity>()
                .WithMany()
                .HasForeignKey(ticket => ticket.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            entity.HasMany(ticket => ticket.Messages)
                .WithOne()
                .HasForeignKey(message => message.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupportMessage>(entity =>
        {
            entity.Property(message => message.Id).ValueGeneratedOnAdd().HasColumnType("uuid");
            entity.Property(message => message.TicketId).HasColumnType("uuid");
            entity.Property(message => message.AuthorId).HasColumnType("uuid");
            entity.Property(message => message.AuthorName).IsRequired().HasMaxLength(250).HasColumnType("character varying(250)");
            entity.Property(message => message.IsAdmin).HasColumnType("boolean");
            entity.Property(message => message.Message).IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)");
            entity.Property(message => message.CreatedAt).HasColumnType("timestamp with time zone");
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => new { message.TicketId, message.CreatedAt });
            entity.ToTable("SupportMessages", "orders");
        });

        modelBuilder.Entity<InventoryConfirmationOutbox>(entity =>
        {
            entity.Property(item => item.OrderId).HasColumnType("uuid");
            entity.Property(item => item.CreatedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(item => item.NextAttemptAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(item => item.AttemptCount).HasColumnType("integer");
            entity.HasKey(item => item.OrderId);
            entity.HasIndex(item => item.NextAttemptAtUtc);
            entity.ToTable("InventoryConfirmationOutbox", "orders");
            entity.HasOne<OrderEntity>()
                .WithOne()
                .HasForeignKey<InventoryConfirmationOutbox>(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
