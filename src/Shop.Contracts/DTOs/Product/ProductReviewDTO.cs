namespace Shop.Contracts.DTOs.Product;

public sealed class ProductReviewDTO
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public bool IsOwnReview { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
