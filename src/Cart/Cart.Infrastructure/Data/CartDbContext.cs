using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cart.Domain.Entities;

using CartEntity = Cart.Domain.Entities.Cart;
namespace Cart.Infrastructure.Data
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
        {
        }

        public DbSet<CartEntity> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("cart");

            modelBuilder.Entity<CartEntity>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.CustomerId).IsRequired();
                entity.Property(c => c.Revision).IsRequired().IsConcurrencyToken();
                entity.HasIndex(c => c.CustomerId).IsUnique();

                entity.HasMany(c => c.Items)
                      .WithOne()
                      .HasForeignKey("CartId")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property<Guid>("CartId").IsRequired();
                entity.Property(i => i.ProductId).IsRequired();
                entity.Property(i => i.ProductNameSnapshot).IsRequired();
                entity.Property(i => i.UnitPriceSnapshot).HasPrecision(18, 2);
                entity.Property(i => i.Quantity).IsRequired();
                entity.Property(i => i.IsAvailable).HasDefaultValue(true);
            });
        }
    }
}
