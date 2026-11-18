namespace Product.Domain.Entities;

public sealed class ProductReview
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string ReviewerName { get; private set; } = string.Empty;
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public bool IsHidden { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ProductReview() { }

    public static ProductReview Create(
        Guid productId,
        Guid userId,
        string reviewerName,
        int rating,
        string? comment)
    {
        Validate(productId, userId, reviewerName, rating, comment);
        var now = DateTime.UtcNow;

        return new ProductReview
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            ReviewerName = reviewerName.Trim(),
            Rating = rating,
            Comment = NormalizeComment(comment),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string reviewerName, int rating, string? comment)
    {
        Validate(ProductId, UserId, reviewerName, rating, comment);
        ReviewerName = reviewerName.Trim();
        Rating = rating;
        Comment = NormalizeComment(comment);
        IsHidden = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Hide()
    {
        IsHidden = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void Validate(
        Guid productId,
        Guid userId,
        string reviewerName,
        int rating,
        string? comment)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(reviewerName))
            throw new ArgumentException("Reviewer name is required.", nameof(reviewerName));
        if (reviewerName.Trim().Length > 200)
            throw new ArgumentOutOfRangeException(nameof(reviewerName), "Reviewer name cannot exceed 200 characters.");
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        if (comment?.Length > 2000)
            throw new ArgumentOutOfRangeException(nameof(comment), "Comment cannot exceed 2000 characters.");
    }

    private static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
}
