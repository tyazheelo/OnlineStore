using Shop.Contracts.DTOs.Product;
using Shop.Contracts.Requests.Product;
using Product.Application.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Product.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<ProductDTO> CreateProductAsync(CreateProductRequest request);
        Task<ProductDTO> CreateProductAsync(CreateProductCommand command);
        Task<ProductDTO?> UpdateProductAsync(Guid id, UpdateProductRequest request);
        Task<ProductDTO?> UpdateProductAsync(Guid id, UpdateProductCommand command);
        Task<ProductDTO?> UpdateStockAsync(Guid id, int stockChange);
        Task<ProductDTO?> ActivateProductAsync(Guid id);
        Task<ProductDTO?> DeactivateProductAsync(Guid id);
        Task<ProductDTO?> GetProductByIdAsync(Guid id);
        Task<List<ProductDTO>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> ids);
        Task<List<CategoryDTO>> GetCategoriesAsync();
        Task<CategoryDTO?> CreateCategoryAsync(string name, string? nameEn);
        Task<List<ProductReviewDTO>?> GetReviewsAsync(Guid productId, Guid? viewerUserId);
        Task<ProductReviewDTO?> UpsertReviewAsync(
            Guid productId,
            Guid userId,
            string reviewerName,
            int rating,
            string? comment);
        Task<ProductReviewReportDTO?> CreateReviewReportAsync(
            Guid productId,
            Guid reviewId,
            Guid reporterUserId,
            IReadOnlyCollection<string> reasons,
            string? otherDetails);
        Task<List<ProductReviewReportDTO>> GetPendingReviewReportsAsync();
        Task<bool> ResolveReviewReportAsync(Guid reportId, bool hideReview, Guid moderatorUserId);
        Task<bool> HideReviewAsync(Guid reviewId, Guid moderatorUserId);
        Task<ProductDTO?> GetProductByNameAsync(string name);
        Task<List<ProductDTO>> GetAllProductsAsync(
            Guid? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? tag = null);
        Task<List<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<List<ProductDTO>> GetActiveProductsAsync();
        Task DeleteProductAsync(Guid id);
    }
}
