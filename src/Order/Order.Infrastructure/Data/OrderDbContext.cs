using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using System.Threading.Tasks;

using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<OrderEntity> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
        public DbSet<InventoryConfirmationOutbox> InventoryConfirmationOutbox => Set<InventoryConfirmationOutbox>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Разделяем таблицы Order модуля в схеме "orders"
            modelBuilder.HasDefaultSchema("orders");

            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(o => o.Status).HasConversion<string>();
                entity.Property(o => o.PaymentType).HasConversion<string>();
                entity.HasIndex(o => new { o.Status, o.CreatedAt });
                entity.HasIndex(o => o.CreatedAt);

                entity.OwnsOne(o => o.DeliveryAddress, address =>
                {
                    address.Property(a => a.Country).HasColumnName("Country").IsRequired();
                    address.Property(a => a.State).HasColumnName("State");
                    address.Property(a => a.City).HasColumnName("City").IsRequired();
                    address.Property(a => a.Street).HasColumnName("Street").IsRequired();
                    address.Property(a => a.PostalCode).HasColumnName("PostalCode").IsRequired();
                });

                entity.HasMany(o => o.Items)
                      .WithOne()
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(i => i.Price).HasPrecision(18, 2);
            });

            modelBuilder.Entity<SupportTicket>(entity =>
            {
                entity.HasKey(ticket => ticket.Id);
                entity.Property(ticket => ticket.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(ticket => ticket.CustomerName).IsRequired().HasMaxLength(250);
                entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(ticket => ticket.CustomerId);
                entity.HasIndex(ticket => new { ticket.Status, ticket.CreatedAt });
                entity.HasOne<OrderEntity>()
                    .WithMany()
                    .HasForeignKey(ticket => ticket.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(ticket => ticket.Messages)
                    .WithOne()
                    .HasForeignKey(message => message.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SupportMessage>(entity =>
            {
                entity.HasKey(message => message.Id);
                entity.Property(message => message.AuthorName).IsRequired().HasMaxLength(250);
                entity.Property(message => message.Message).IsRequired().HasMaxLength(2000);
                entity.HasIndex(message => new { message.TicketId, message.CreatedAt });
            });

            modelBuilder.Entity<InventoryConfirmationOutbox>(entity =>
            {
                entity.HasKey(item => item.OrderId);
                entity.Property(item => item.CreatedAtUtc).IsRequired();
                entity.Property(item => item.NextAttemptAtUtc).IsRequired();
                entity.Property(item => item.AttemptCount).IsRequired();
                entity.HasIndex(item => item.NextAttemptAtUtc);
                entity.HasOne<OrderEntity>()
                    .WithOne()
                    .HasForeignKey<InventoryConfirmationOutbox>(item => item.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

    }
}
