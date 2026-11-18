using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Product.Domain.Entities;
using System.Text.Json;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.Infrastructure.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ProductEntity> Products { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductReviewReport> ProductReviewReports { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<StockReservation> StockReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // выделяем схему для модуля Product
            modelBuilder.HasDefaultSchema("products");

            modelBuilder.Entity<ProductEntity>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.NameEn).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.DescriptionEn).IsRequired().HasMaxLength(1000);
                entity.Property(p => p.ImageUrl).HasMaxLength(2048);
                var translatedSpecificationsConverter =
                    new ValueConverter<Dictionary<string, ProductSpecificationTranslation>, string>(
                        translations => JsonSerializer.Serialize(translations, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<Dictionary<string, ProductSpecificationTranslation>>(
                            json,
                            (JsonSerializerOptions?)null)
                            ?? new Dictionary<string, ProductSpecificationTranslation>());
                var translatedSpecificationsComparer =
                    new ValueComparer<Dictionary<string, ProductSpecificationTranslation>>(
                        (left, right) => left != null && right != null
                            && left.OrderBy(item => item.Key, StringComparer.Ordinal)
                                .SequenceEqual(right.OrderBy(item => item.Key, StringComparer.Ordinal)),
                        translations => translations.OrderBy(item => item.Key, StringComparer.Ordinal)
                            .Aggregate(0, (hash, item) => HashCode.Combine(hash, item.Key, item.Value)),
                        translations => translations.ToDictionary(
                            item => item.Key,
                            item => item.Value,
                            StringComparer.Ordinal));
                entity.Property(p => p.SpecificationsEn)
                    .HasConversion(translatedSpecificationsConverter)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb")
                    .Metadata.SetValueComparer(translatedSpecificationsComparer);
                var specificationsConverter = new ValueConverter<Dictionary<string, string>, string>(
                    specifications => JsonSerializer.Serialize(specifications, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>());
                var specificationsComparer = new ValueComparer<Dictionary<string, string>>(
                    (left, right) => left != null && right != null
                        && left.OrderBy(item => item.Key, StringComparer.Ordinal)
                            .SequenceEqual(right.OrderBy(item => item.Key, StringComparer.Ordinal)),
                    specifications => specifications.OrderBy(item => item.Key, StringComparer.Ordinal)
                        .Aggregate(0, (hash, item) => HashCode.Combine(hash, item.Key, item.Value)),
                    specifications => specifications.ToDictionary(
                        item => item.Key,
                        item => item.Value,
                        StringComparer.Ordinal));
                entity.Property(p => p.Specifications)
                    .HasConversion(specificationsConverter)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb")
                    .Metadata.SetValueComparer(specificationsComparer);
                var tagsConverter = new ValueConverter<List<string>, string>(
                    tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new List<string>());
                var tagsComparer = new ValueComparer<List<string>>(
                    (left, right) => left != null && right != null
                        && left.SequenceEqual(right, StringComparer.Ordinal),
                    tags => tags.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag)),
                    tags => tags.ToList());
                entity.Property(p => p.Tags)
                    .HasConversion(tagsConverter)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb")
                    .Metadata.SetValueComparer(tagsComparer);
                entity.Property(p => p.CategoryId).IsRequired();
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.StockQuantity).IsRequired();
                entity.Property(p => p.IsActive).IsRequired();
                entity.HasOne(p => p.Category)
                    .WithMany(category => category.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(category => category.Id);
                entity.Property(category => category.Name).IsRequired().HasMaxLength(100);
                entity.Property(category => category.NameEn).IsRequired().HasMaxLength(100);
                entity.Property(category => category.NormalizedName).IsRequired().HasMaxLength(100);
                entity.HasIndex(category => category.NormalizedName).IsUnique();
                entity.HasData(
                    new { Id = Guid.Parse("8a173dd5-72c2-4e0d-9e1e-68d3a3160001"), Name = "Аксессуары", NameEn = "Accessories", NormalizedName = "АКСЕССУАРЫ" },
                    new { Id = Guid.Parse("8a173dd5-72c2-4e0d-9e1e-68d3a3160002"), Name = "Для дома", NameEn = "Home", NormalizedName = "ДЛЯ ДОМА" },
                    new { Id = Guid.Parse("8a173dd5-72c2-4e0d-9e1e-68d3a3160003"), Name = "Техника", NameEn = "Electronics", NormalizedName = "ТЕХНИКА" },
                    new { Id = Guid.Parse("8a173dd5-72c2-4e0d-9e1e-68d3a3160004"), Name = "Без категории", NameEn = "Uncategorized", NormalizedName = "БЕЗ КАТЕГОРИИ" });
            });

            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.HasKey(review => review.Id);
                entity.Property(review => review.ReviewerName).IsRequired().HasMaxLength(200);
                entity.Property(review => review.Rating).IsRequired();
                entity.Property(review => review.Comment).HasMaxLength(2000);
                entity.Property(review => review.IsHidden).IsRequired();
                entity.Property(review => review.CreatedAtUtc).IsRequired();
                entity.Property(review => review.UpdatedAtUtc).IsRequired();
                entity.HasIndex(review => new { review.ProductId, review.UserId }).IsUnique();
                entity.HasIndex(review => new { review.ProductId, review.CreatedAtUtc });
                entity.HasOne<ProductEntity>()
                    .WithMany()
                    .HasForeignKey(review => review.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_ProductReviews_Rating",
                    "\"Rating\" >= 1 AND \"Rating\" <= 5"));
            });

            modelBuilder.Entity<ProductReviewReport>(entity =>
            {
                entity.HasKey(report => report.Id);
                entity.Property(report => report.ReasonsJson)
                    .HasColumnName("Reasons")
                    .HasColumnType("jsonb")
                    .IsRequired();
                entity.Property(report => report.OtherDetails).HasMaxLength(1000);
                entity.Property(report => report.Status).HasConversion<string>().IsRequired();
                entity.Property(report => report.CreatedAtUtc).IsRequired();
                entity.HasIndex(report => new { report.ReviewId, report.ReporterUserId })
                    .IsUnique()
                    .HasFilter("\"Status\" = 'Pending'");
                entity.HasIndex(report => new { report.Status, report.CreatedAtUtc });
                entity.HasOne<ProductReview>()
                    .WithMany()
                    .HasForeignKey(report => report.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable("ProductReviewReports");
            });

            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.HasKey(reservation => reservation.OrderId);
                entity.Property(reservation => reservation.Status).HasConversion<string>().IsRequired();
                entity.HasMany(reservation => reservation.Items)
                    .WithOne()
                    .HasForeignKey(item => item.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockReservationItem>(entity =>
            {
                entity.ToTable("StockReservationItems");
                entity.HasKey(item => new { item.OrderId, item.ProductId });
                entity.Property(item => item.Quantity).IsRequired();
            });
        }
    }
}
