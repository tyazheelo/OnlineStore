using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProductEntity = Product.Domain.Entities.Product;
using Product.Domain.Entities;
using Product.Application.Models;
using Shop.Contracts.DTOs.Product;

namespace Product.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<ProductEntity?> GetByIdAsync(Guid id);
        Task<List<ProductEntity>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);
        Task<List<ProductEntity>> GetAllAsync(
            Guid? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null);
        Task<List<Category>> GetCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(Guid categoryId);
        Task<Category?> GetCategoryByNormalizedNameAsync(string normalizedName);
        Task<bool> AddCategoryAsync(Category category);
        Task<Dictionary<Guid, ProductRatingSummary>> GetRatingSummariesAsync(IReadOnlyCollection<Guid> productIds);
        Task<List<ProductReviewDTO>> GetReviewsAsync(Guid productId, Guid? viewerUserId);
        Task<ProductReview> UpsertReviewAsync(Guid productId, Guid userId, string reviewerName, int rating, string? comment);
        Task<ProductReview?> GetReviewByIdAsync(Guid reviewId);
        Task<bool> HasReportedReviewAsync(Guid reviewId, Guid reporterUserId);
        Task AddReviewReportAsync(ProductReviewReport report);
        Task<List<ProductReviewReportDTO>> GetPendingReviewReportsAsync();
        Task<bool> ResolveReviewReportAsync(Guid reportId, bool hideReview, Guid moderatorUserId);
        Task<bool> HideReviewAsync(Guid reviewId, Guid moderatorUserId);
        Task AddAsync(ProductEntity product);
        Task UpdateAsync(ProductEntity product);
        Task DeleteAsync(Guid id);
        Task ReserveStockAsync(StockReservation reservation, CancellationToken cancellationToken = default);
        Task ConfirmReservationAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task ReleaseReservationAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<bool> ExpireReservationAsync(Guid orderId, DateTime cutoffUtc, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetExpiredReservationOrderIdsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
        Task<bool> IsReservationConfirmedAsync(Guid orderId, CancellationToken cancellationToken = default);
    }
}
