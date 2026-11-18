using Product.Application.Interfaces.Repositories;
using Product.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Domain.Enums;
using Product.Application.Models;
using Shop.Contracts.DTOs.Product;
using Npgsql;
using ProductEntity = Product.Domain.Entities.Product;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Product.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;

        public ProductRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ProductEntity product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task<ProductEntity?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(product => product.Category)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<List<ProductEntity>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
        {
            return await _context.Products
                .Include(product => product.Category)
                .Where(product => ids.Contains(product.Id))
                .ToListAsync();
        }

        public async Task<List<ProductEntity>> GetAllAsync(
            Guid? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null)
        {
            IQueryable<ProductEntity> query = _context.Products.Include(product => product.Category);
            if (categoryId.HasValue)
                query = query.Where(product => product.CategoryId == categoryId.Value);
            if (minPrice.HasValue)
                query = query.Where(product => product.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(product => product.Price <= maxPrice.Value);

            return await query.ToListAsync();
        }

        public Task<List<Category>> GetCategoriesAsync()
        {
            return _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();
        }

        public Task<Category?> GetCategoryByIdAsync(Guid categoryId)
        {
            return _context.Categories
                .SingleOrDefaultAsync(category => category.Id == categoryId);
        }

        public Task<Category?> GetCategoryByNormalizedNameAsync(string normalizedName)
        {
            return _context.Categories
                .AsNoTracking()
                .SingleOrDefaultAsync(category => category.NormalizedName == normalizedName);
        }

        public async Task<bool> AddCategoryAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "IX_Categories_NormalizedName"
                })
            {
                _context.Entry(category).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<Dictionary<Guid, ProductRatingSummary>> GetRatingSummariesAsync(
            IReadOnlyCollection<Guid> productIds)
        {
            if (productIds.Count == 0)
                return new Dictionary<Guid, ProductRatingSummary>();

            return await _context.ProductReviews
                .AsNoTracking()
                .Where(review => productIds.Contains(review.ProductId) && !review.IsHidden)
                .GroupBy(review => review.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    AverageRating = group.Average(review => (decimal)review.Rating),
                    ReviewCount = group.Count()
                })
                .ToDictionaryAsync(
                    summary => summary.ProductId,
                    summary => new ProductRatingSummary(summary.AverageRating, summary.ReviewCount));
        }

        public Task<List<ProductReviewDTO>> GetReviewsAsync(Guid productId, Guid? viewerUserId)
        {
            return _context.ProductReviews
                .AsNoTracking()
                .Where(review => review.ProductId == productId && !review.IsHidden)
                .OrderByDescending(review => review.CreatedAtUtc)
                .Select(review => new ProductReviewDTO
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    IsOwnReview = viewerUserId.HasValue && review.UserId == viewerUserId.Value,
                    ReviewerName = review.ReviewerName,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAtUtc = review.CreatedAtUtc,
                    UpdatedAtUtc = review.UpdatedAtUtc
                })
                .ToListAsync();
        }

        public Task<ProductReview?> GetReviewByIdAsync(Guid reviewId)
        {
            return _context.ProductReviews.FirstOrDefaultAsync(review => review.Id == reviewId);
        }

        public Task<bool> HasReportedReviewAsync(Guid reviewId, Guid reporterUserId)
        {
            return _context.ProductReviewReports.AnyAsync(report =>
                report.ReviewId == reviewId
                && report.ReporterUserId == reporterUserId
                && report.Status == ProductReviewReportStatus.Pending);
        }

        public async Task AddReviewReportAsync(ProductReviewReport report)
        {
            await _context.ProductReviewReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductReviewReportDTO>> GetPendingReviewReportsAsync()
        {
            var pendingReports = await (
                from report in _context.ProductReviewReports.AsNoTracking()
                join review in _context.ProductReviews.AsNoTracking() on report.ReviewId equals review.Id
                join product in _context.Products.AsNoTracking() on review.ProductId equals product.Id
                where report.Status == ProductReviewReportStatus.Pending
                orderby report.CreatedAtUtc
                select new { Report = report, Review = review, Product = product })
                .ToListAsync();

            return pendingReports.Select(item => new ProductReviewReportDTO
            {
                Id = item.Report.Id,
                ReviewId = item.Review.Id,
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                ReviewerName = item.Review.ReviewerName,
                Rating = item.Review.Rating,
                Comment = item.Review.Comment,
                ReviewCreatedAtUtc = item.Review.CreatedAtUtc,
                ReporterUserId = item.Report.ReporterUserId,
                Reasons = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                    item.Report.ReasonsJson,
                    (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>(),
                OtherDetails = item.Report.OtherDetails,
                ReportedAtUtc = item.Report.CreatedAtUtc
            }).ToList();
        }

        public async Task<bool> ResolveReviewReportAsync(Guid reportId, bool hideReview, Guid moderatorUserId)
        {
            var report = await _context.ProductReviewReports
                .FirstOrDefaultAsync(item => item.Id == reportId && item.Status == ProductReviewReportStatus.Pending);
            if (report is null)
                return false;

            var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == report.ReviewId);
            if (review is null)
                return false;

            report.Resolve(hideReview, moderatorUserId);
            if (hideReview)
            {
                review.Hide();
                var otherReports = await _context.ProductReviewReports
                    .Where(item => item.ReviewId == review.Id && item.Status == ProductReviewReportStatus.Pending)
                    .ToListAsync();
                foreach (var otherReport in otherReports)
                    otherReport.MarkHidden(moderatorUserId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HideReviewAsync(Guid reviewId, Guid moderatorUserId)
        {
            var review = await _context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId);
            if (review is null)
                return false;

            review.Hide();
            var pendingReports = await _context.ProductReviewReports
                .Where(report => report.ReviewId == review.Id && report.Status == ProductReviewReportStatus.Pending)
                .ToListAsync();
            foreach (var report in pendingReports)
                report.MarkHidden(moderatorUserId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ProductReview> UpsertReviewAsync(
            Guid productId,
            Guid userId,
            string reviewerName,
            int rating,
            string? comment)
        {
            var review = await _context.ProductReviews
                .SingleOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId);

            if (review is null)
            {
                review = ProductReview.Create(productId, userId, reviewerName, rating, comment);
                await _context.ProductReviews.AddAsync(review);
            }
            else
            {
                review.Update(reviewerName, rating, comment);
            }

            await _context.SaveChangesAsync();
            return review;
        }

        public async Task UpdateAsync(ProductEntity product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await GetByIdAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReserveStockAsync(StockReservation reservation, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var existing = await _context.StockReservations
                .Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.OrderId == reservation.OrderId, cancellationToken);

            if (existing is not null)
            {
                if (existing.Status != StockReservationStatus.Released
                    && existing.Matches(reservation.Items.Select(item => (item.ProductId, item.Quantity))))
                    return;

                throw new InvalidOperationException("An incompatible stock reservation already exists for this order.");
            }

            foreach (var item in reservation.Items.OrderBy(item => item.ProductId))
            {
                var productExists = await _context.Products
                    .AnyAsync(product => product.Id == item.ProductId, cancellationToken);
                if (!productExists)
                    throw new KeyNotFoundException($"Product {item.ProductId} was not found.");

                var updated = await _context.Products
                    .Where(product => product.Id == item.ProductId
                        && product.IsActive
                        && product.StockQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(product => product.StockQuantity, product => product.StockQuantity - item.Quantity),
                        cancellationToken);

                if (updated == 0)
                    throw new InvalidOperationException($"Product {item.ProductId} is inactive or has insufficient stock.");
            }

            await _context.StockReservations.AddAsync(reservation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task ConfirmReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var status = await _context.StockReservations
                .AsNoTracking()
                .Where(item => item.OrderId == orderId)
                .Select(item => (StockReservationStatus?)item.Status)
                .SingleOrDefaultAsync(cancellationToken);
            if (status is null)
                throw new KeyNotFoundException($"Stock reservation for order {orderId} was not found.");

            if (status == StockReservationStatus.Confirmed)
                return;
            if (status == StockReservationStatus.Released)
                throw new InvalidOperationException("A released stock reservation cannot be confirmed.");

            var updated = await _context.StockReservations
                .Where(item => item.OrderId == orderId && item.Status == StockReservationStatus.Reserved)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.Status, StockReservationStatus.Confirmed), cancellationToken);

            if (updated == 1)
                return;

            var finalStatus = await _context.StockReservations
                .SingleOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);
            if (finalStatus?.Status != StockReservationStatus.Confirmed)
                throw new InvalidOperationException("The stock reservation changed while being confirmed.");
        }

        public async Task ReleaseReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                var reservation = await _context.StockReservations
                    .Include(item => item.Items)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);

                if (reservation is null || reservation.Status == StockReservationStatus.Released)
                    return;

                var updated = await _context.StockReservations
                    .Where(item => item.OrderId == orderId && item.Status == reservation.Status)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(item => item.Status, StockReservationStatus.Released), cancellationToken);

                if (updated == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                foreach (var item in reservation.Items)
                {
                    await _context.Products
                        .Where(product => product.Id == item.ProductId)
                        .ExecuteUpdateAsync(update => update
                            .SetProperty(product => product.StockQuantity, product => product.StockQuantity + item.Quantity),
                            cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return;
            }

            throw new InvalidOperationException("The stock reservation changed repeatedly while being released.");
        }

        public async Task<bool> ExpireReservationAsync(
            Guid orderId,
            DateTime cutoffUtc,
            CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                var reservation = await _context.StockReservations
                    .Include(item => item.Items)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);

                if (reservation is null || reservation.CreatedAtUtc > cutoffUtc)
                    return false;
                if (reservation.Status == StockReservationStatus.Released)
                    return true;
                if (reservation.Status != StockReservationStatus.Reserved)
                    return false;

                var updated = await _context.StockReservations
                    .Where(item => item.OrderId == orderId
                        && item.Status == StockReservationStatus.Reserved
                        && item.CreatedAtUtc <= cutoffUtc)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(item => item.Status, StockReservationStatus.Released), cancellationToken);

                if (updated == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                foreach (var item in reservation.Items)
                {
                    await _context.Products
                        .Where(product => product.Id == item.ProductId)
                        .ExecuteUpdateAsync(update => update
                            .SetProperty(product => product.StockQuantity, product => product.StockQuantity + item.Quantity),
                            cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return true;
            }

            var finalStatus = await _context.StockReservations
                .Where(item => item.OrderId == orderId)
                .Select(item => (StockReservationStatus?)item.Status)
                .SingleOrDefaultAsync(cancellationToken);
            return finalStatus == StockReservationStatus.Released;
        }

        public Task<List<Guid>> GetExpiredReservationOrderIdsAsync(
            DateTime cutoffUtc,
            CancellationToken cancellationToken = default)
        {
            return _context.StockReservations
                .Where(reservation => reservation.Status == StockReservationStatus.Reserved
                    && reservation.CreatedAtUtc <= cutoffUtc)
                .Select(reservation => reservation.OrderId)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> IsReservationConfirmedAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return _context.StockReservations
                .AnyAsync(reservation => reservation.OrderId == orderId
                    && reservation.Status == StockReservationStatus.Confirmed,
                    cancellationToken);
        }
    }
}
