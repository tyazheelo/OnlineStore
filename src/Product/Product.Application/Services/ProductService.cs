using Product.Application.Interfaces.Repositories;
using Product.Application.Interfaces.Services;
using Product.Application.Models;
using Product.Domain.Entities;
using Shop.Contracts.DTOs.Product;
using Shop.Contracts.Requests.Product;
using Product.Application.Commands;
using ProductEntity = Product.Domain.Entities.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Product.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProductDTO> CreateProductAsync(CreateProductRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var command = new CreateProductCommand
            {
                Name = request.Name,
                NameEn = request.NameEn,
                Description = request.Description,
                DescriptionEn = request.DescriptionEn,
                ImageUrl = request.ImageUrl,
                Specifications = request.Specifications,
                SpecificationsEn = request.SpecificationsEn?.ToDictionary(
                    item => item.Key,
                    item => new ProductSpecificationTranslation(item.Value?.Name ?? string.Empty, item.Value?.Value ?? string.Empty))
                    ?? new Dictionary<string, ProductSpecificationTranslation>(),
                Tags = request.Tags,
                CategoryId = request.CategoryId,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            return await CreateProductAsync(command).ConfigureAwait(false);
        }

        public async Task<ProductDTO> CreateProductAsync(CreateProductCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.Name)) throw new ArgumentException("Name is required", nameof(command.Name));
            if (string.IsNullOrWhiteSpace(command.Description)) throw new ArgumentException("Description is required", nameof(command.Description));
            var category = await _repo.GetCategoryByIdAsync(command.CategoryId).ConfigureAwait(false);
            if (category == null) throw new ArgumentException("Category was not found", nameof(command.CategoryId));
            if (command.Price <= 0) throw new ArgumentOutOfRangeException(nameof(command.Price), "Price must be greater than zero");
            if (command.StockQuantity < 0) throw new ArgumentOutOfRangeException(nameof(command.StockQuantity), "Stock quantity cannot be negative");

            var product = ProductEntity.Create(
                command.Name,
                command.Description,
                category,
                command.Price,
                command.StockQuantity,
                command.ImageUrl,
                command.Specifications,
                command.Tags,
                command.NameEn,
                command.DescriptionEn,
                command.SpecificationsEn);

            await _repo.AddAsync(product).ConfigureAwait(false);

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<ProductDTO?> UpdateProductAsync(Guid id, UpdateProductRequest request)
        {
            var command = new UpdateProductCommand
            {
                Name = request?.Name ?? string.Empty,
                NameEn = request?.NameEn ?? string.Empty,
                Description = request?.Description ?? string.Empty,
                DescriptionEn = request?.DescriptionEn ?? string.Empty,
                ImageUrl = request?.ImageUrl,
                Specifications = request?.Specifications ?? new Dictionary<string, string>(),
                SpecificationsEn = request?.SpecificationsEn?.ToDictionary(
                    item => item.Key,
                    item => new ProductSpecificationTranslation(item.Value?.Name ?? string.Empty, item.Value?.Value ?? string.Empty))
                    ?? new Dictionary<string, ProductSpecificationTranslation>(),
                Tags = request?.Tags ?? new List<string>(),
                CategoryId = request?.CategoryId ?? Guid.Empty,
                Price = request?.Price ?? 0m
            };

            return await UpdateProductAsync(id, command).ConfigureAwait(false);
        }

        public async Task<ProductDTO?> UpdateProductAsync(Guid id, UpdateProductCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.Name)) throw new ArgumentException("Name is required", nameof(command.Name));
            if (string.IsNullOrWhiteSpace(command.Description)) throw new ArgumentException("Description is required", nameof(command.Description));
            var category = await _repo.GetCategoryByIdAsync(command.CategoryId).ConfigureAwait(false);
            if (category == null) throw new ArgumentException("Category was not found", nameof(command.CategoryId));
            if (command.Price <= 0) throw new ArgumentOutOfRangeException(nameof(command.Price), "Price must be greater than zero");

            var product = await _repo.GetByIdAsync(id);
            if (product == null) return null;

            product.Update(
                command.Name,
                command.Description,
                category,
                command.Price,
                command.ImageUrl,
                command.Specifications,
                command.Tags,
                command.NameEn,
                command.DescriptionEn,
                command.SpecificationsEn);

            await _repo.UpdateAsync(product).ConfigureAwait(false);

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<ProductDTO?> UpdateStockAsync(Guid id, int stockChange)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return null;

            product.UpdateStock(stockChange);

            await _repo.UpdateAsync(product).ConfigureAwait(false);

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<ProductDTO?> ActivateProductAsync(Guid id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return null;

            product.Activate();

            await _repo.UpdateAsync(product).ConfigureAwait(false);

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<ProductDTO?> DeactivateProductAsync(Guid id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return null;

            product.Deactivate();

            await _repo.UpdateAsync(product).ConfigureAwait(false);

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<ProductDTO?> GetProductByIdAsync(Guid id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                return null;

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<List<ProductDTO>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> ids)
        {
            if (ids.Count == 0)
                return new List<ProductDTO>();

            var products = await _repo.GetByIdsAsync(ids.Distinct().ToArray());
            return await MapToDtosAsync(products).ConfigureAwait(false);
        }

        public async Task<List<CategoryDTO>> GetCategoriesAsync()
        {
            var categories = await _repo.GetCategoriesAsync().ConfigureAwait(false);
            return categories.Select(category => new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                NameEn = category.NameEn
            }).ToList();
        }

        public async Task<CategoryDTO?> CreateCategoryAsync(string name, string? nameEn)
        {
            var category = Category.Create(name, nameEn);
            if (await _repo.GetCategoryByNormalizedNameAsync(category.NormalizedName).ConfigureAwait(false) != null)
                return null;

            if (!await _repo.AddCategoryAsync(category).ConfigureAwait(false))
                return null;

            return new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                NameEn = category.NameEn
            };
        }

        public async Task<List<ProductReviewDTO>?> GetReviewsAsync(Guid productId, Guid? viewerUserId)
        {
            if (await _repo.GetByIdAsync(productId).ConfigureAwait(false) == null)
                return null;

            return await _repo.GetReviewsAsync(productId, viewerUserId).ConfigureAwait(false);
        }

        public async Task<ProductReviewDTO?> UpsertReviewAsync(
            Guid productId,
            Guid userId,
            string reviewerName,
            int rating,
            string? comment)
        {
            if (await _repo.GetByIdAsync(productId).ConfigureAwait(false) == null)
                return null;

            var review = await _repo.UpsertReviewAsync(
                productId,
                userId,
                reviewerName,
                rating,
                comment).ConfigureAwait(false);

            return new ProductReviewDTO
            {
                Id = review.Id,
                ProductId = review.ProductId,
                IsOwnReview = true,
                ReviewerName = review.ReviewerName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAtUtc = review.CreatedAtUtc,
                UpdatedAtUtc = review.UpdatedAtUtc
            };
        }

        public async Task<ProductReviewReportDTO?> CreateReviewReportAsync(
            Guid productId,
            Guid reviewId,
            Guid reporterUserId,
            IReadOnlyCollection<string> reasons,
            string? otherDetails)
        {
            var review = await _repo.GetReviewByIdAsync(reviewId).ConfigureAwait(false);
            if (review is null || review.ProductId != productId || review.IsHidden)
                return null;
            if (review.UserId == reporterUserId)
                throw new ArgumentException("You cannot report your own review.");
            if (reasons.Count == 0 || reasons.Any(reason => !ProductReviewReportReasons.All.Contains(reason, StringComparer.Ordinal)))
                throw new ArgumentException("Select at least one valid report reason.");
            if (reasons.Distinct(StringComparer.Ordinal).Count() != reasons.Count)
                throw new ArgumentException("Report reasons must be unique.");
            if (reasons.Contains(ProductReviewReportReasons.Other, StringComparer.Ordinal)
                && string.IsNullOrWhiteSpace(otherDetails))
                throw new ArgumentException("Please explain the other report reason.");
            if (await _repo.HasReportedReviewAsync(reviewId, reporterUserId).ConfigureAwait(false))
                throw new ArgumentException("You have already reported this review.");

            var report = ProductReviewReport.Create(reviewId, reporterUserId, reasons, otherDetails);
            await _repo.AddReviewReportAsync(report).ConfigureAwait(false);
            return new ProductReviewReportDTO
            {
                Id = report.Id,
                ReviewId = review.Id,
                ProductId = review.ProductId,
                ReviewerName = review.ReviewerName,
                Rating = review.Rating,
                Comment = review.Comment,
                ReviewCreatedAtUtc = review.CreatedAtUtc,
                ReporterUserId = reporterUserId,
                Reasons = reasons.ToList(),
                OtherDetails = report.OtherDetails,
                ReportedAtUtc = report.CreatedAtUtc
            };
        }

        public Task<List<ProductReviewReportDTO>> GetPendingReviewReportsAsync() =>
            _repo.GetPendingReviewReportsAsync();

        public Task<bool> ResolveReviewReportAsync(Guid reportId, bool hideReview, Guid moderatorUserId) =>
            _repo.ResolveReviewReportAsync(reportId, hideReview, moderatorUserId);

        public Task<bool> HideReviewAsync(Guid reviewId, Guid moderatorUserId) =>
            _repo.HideReviewAsync(reviewId, moderatorUserId);

        public async Task<ProductDTO?> GetProductByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));

            var products = await _repo.GetAllAsync();
            var product = products.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (product == null)
                return null;

            return (await MapToDtosAsync([product]).ConfigureAwait(false))[0];
        }

        public async Task<List<ProductDTO>> GetAllProductsAsync(
            Guid? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? tag = null)
        {
            ValidatePriceRange(minPrice, maxPrice);
            var products = await _repo.GetAllAsync(categoryId, minPrice, maxPrice);
            if (!string.IsNullOrWhiteSpace(tag))
            {
                products = products
                    .Where(product => product.Tags.Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            return await MapToDtosAsync(products).ConfigureAwait(false);
        }

        public async Task<List<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            ValidatePriceRange(minPrice, maxPrice);

            var products = await _repo.GetAllAsync(minPrice: minPrice, maxPrice: maxPrice);
            return await MapToDtosAsync(products).ConfigureAwait(false);
        }

        public async Task<List<ProductDTO>> GetActiveProductsAsync()
        {
            var products = await _repo.GetAllAsync();
            var active = products.Where(p => p.IsActive).ToList();
            return await MapToDtosAsync(active).ConfigureAwait(false);
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Product with ID {id} not found");

            await _repo.DeleteAsync(id).ConfigureAwait(false);
        }

        private async Task<List<ProductDTO>> MapToDtosAsync(IReadOnlyCollection<ProductEntity> products)
        {
            var summaries = await _repo.GetRatingSummariesAsync(products.Select(product => product.Id).ToArray())
                .ConfigureAwait(false);

            return products.Select(product =>
            {
                summaries.TryGetValue(product.Id, out var summary);
                return MapToDto(product, summary);
            }).ToList();
        }

        private static ProductDTO MapToDto(ProductEntity product, ProductRatingSummary? ratingSummary)
        {
            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                NameEn = product.NameEn,
                Description = product.Description,
                DescriptionEn = product.DescriptionEn,
                ImageUrl = product.ImageUrl,
                Specifications = new Dictionary<string, string>(product.Specifications),
                SpecificationsEn = product.SpecificationsEn.ToDictionary(
                    item => item.Key,
                    item => new ProductSpecificationTranslationDTO
                    {
                        Name = item.Value.Name,
                        Value = item.Value.Value
                    }),
                Tags = new List<string>(product.Tags),
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                CategoryNameEn = product.Category.NameEn,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                RatingAverage = ratingSummary?.AverageRating ?? 0,
                ReviewCount = ratingSummary?.ReviewCount ?? 0
            };
        }

        private static void ValidatePriceRange(decimal? minPrice, decimal? maxPrice)
        {
            if (minPrice < 0) throw new ArgumentOutOfRangeException(nameof(minPrice));
            if (maxPrice < 0) throw new ArgumentOutOfRangeException(nameof(maxPrice));
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
                throw new ArgumentException("minPrice must be less than or equal to maxPrice");
        }
    }
}
