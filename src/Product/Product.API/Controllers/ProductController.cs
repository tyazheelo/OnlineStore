using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Product.Application.Interfaces.Services;
using Product.API.Mappers;
using Shop.Contracts.Requests.Product;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Product.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService service, ILogger<ProductController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var command = CreateProductMapper.ToCommand(request);
                var product = await _service.CreateProductAsync(command);
                return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while creating product");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await _service.GetCategoriesAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                var category = await _service.CreateCategoryAsync(request.Name, request.NameEn);
                return category is null
                    ? Conflict(new { error = "Категория с таким названием уже существует." })
                    : StatusCode(StatusCodes.Status201Created, category);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while creating a product category");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{productId:guid}/reviews")]
        public async Task<IActionResult> GetReviews(Guid productId)
        {
            Guid? viewerUserId = TryGetCurrentUserId(out var userId) ? userId : null;
            var reviews = await _service.GetReviewsAsync(productId, viewerUserId);
            return reviews is null ? NotFound() : Ok(reviews);
        }

        [Authorize]
        [HttpPut("{productId:guid}/reviews")]
        public async Task<IActionResult> UpsertReview(
            Guid productId,
            [FromBody] UpsertProductReviewRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdValue, out var userId) || userId == Guid.Empty)
                return Unauthorized();

            var reviewerName = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(reviewerName))
                return Unauthorized();

            try
            {
                var review = await _service.UpsertReviewAsync(
                    productId,
                    userId,
                    reviewerName,
                    request.Rating,
                    request.Comment);
                return review is null ? NotFound() : Ok(review);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid review submitted for product {ProductId}", productId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{productId:guid}/reviews/{reviewId:guid}/reports")]
        public async Task<IActionResult> ReportReview(
            Guid productId,
            Guid reviewId,
            [FromBody] CreateProductReviewReportRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized();

            try
            {
                var report = await _service.CreateReviewReportAsync(
                    productId,
                    reviewId,
                    userId,
                    request.Reasons,
                    request.OtherDetails);
                return report is null ? NotFound() : Ok(report);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("review-reports")]
        public async Task<IActionResult> GetPendingReviewReports()
        {
            return Ok(await _service.GetPendingReviewReportsAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("review-reports/{reportId:guid}/resolve")]
        public async Task<IActionResult> ResolveReviewReport(
            Guid reportId,
            [FromBody] ResolveProductReviewReportRequest request)
        {
            if (!TryGetCurrentUserId(out var moderatorUserId))
                return Unauthorized();

            var resolved = await _service.ResolveReviewReportAsync(
                reportId,
                request.HideReview,
                moderatorUserId);
            return resolved ? NoContent() : NotFound();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("reviews/{reviewId:guid}")]
        public async Task<IActionResult> HideReview(Guid reviewId)
        {
            if (!TryGetCurrentUserId(out var moderatorUserId))
                return Unauthorized();

            var hidden = await _service.HideReviewAsync(reviewId, moderatorUserId);
            return hidden ? NoContent() : NotFound();
        }

        [HttpGet("by-ids")]
        public async Task<IActionResult> GetByIds([FromQuery] List<Guid> ids)
        {
            if (ids.Count == 0)
                return BadRequest(new { error = "At least one product ID is required." });
            if (ids.Count > 100)
                return BadRequest(new { error = "At most 100 product IDs can be requested at once." });

            return Ok(await _service.GetProductsByIdsAsync(ids));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? tag)
        {
            if (categoryId == Guid.Empty)
                return BadRequest(new { error = "Category ID cannot be empty." });
            if (minPrice < 0 || maxPrice < 0)
                return BadRequest(new { error = "Price filters cannot be negative." });
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
                return BadRequest(new { error = "Minimum price cannot exceed maximum price." });

            var products = await _service.GetAllProductsAsync(categoryId, minPrice, maxPrice, tag);
            return Ok(products);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var command = UpdateProductMapper.ToCommand(request);

            try
            {
                var updated = await _service.UpdateProductAsync(id, command);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed while updating product {ProductId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(Guid id, [FromQuery] int change)
        {
            var updated = await _service.UpdateStockAsync(id, change);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var updated = await _service.ActivateProductAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var updated = await _service.DeactivateProductAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _service.DeleteProductAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            return Guid.TryParse(userIdValue, out userId) && userId != Guid.Empty;
        }
    }
}
