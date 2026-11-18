using Identity.Infrastructure.Authentication;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(user => user.LastName).IsRequired().HasMaxLength(100);
            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("EmailIndex");
        });

        builder.Entity<SavedAddress>(entity =>
        {
            entity.HasKey(address => address.Id);
            entity.Property(address => address.Label).IsRequired().HasMaxLength(50);
            entity.Property(address => address.Country).IsRequired().HasMaxLength(100);
            entity.Property(address => address.State).HasMaxLength(100);
            entity.Property(address => address.City).IsRequired().HasMaxLength(100);
            entity.Property(address => address.Street).IsRequired().HasMaxLength(200);
            entity.Property(address => address.PostalCode).IsRequired().HasMaxLength(20);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(address => address.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(address => new { address.UserId, address.IsDefault });
        });
    }
}
