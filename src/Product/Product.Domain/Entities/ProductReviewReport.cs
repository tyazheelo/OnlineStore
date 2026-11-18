namespace Product.Domain.Entities;

public static class ProductReviewReportReasons
{
    public const string Profanity = "Profanity";
    public const string NotAboutProduct = "NotAboutProduct";
    public const string SexualContent = "SexualContent";
    public const string Spam = "Spam";
    public const string Harassment = "Harassment";
    public const string Other = "Other";

    public static IReadOnlyCollection<string> All { get; } =
    [
        Profanity,
        NotAboutProduct,
        SexualContent,
        Spam,
        Harassment,
        Other
    ];
}

public enum ProductReviewReportStatus
{
    Pending,
    Dismissed,
    Hidden
}

public sealed class ProductReviewReport
{
    public Guid Id { get; private set; }
    public Guid ReviewId { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public string ReasonsJson { get; private set; } = "[]";
    public string? OtherDetails { get; private set; }
    public ProductReviewReportStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ModeratorUserId { get; private set; }

    private ProductReviewReport() { }

    public static ProductReviewReport Create(
        Guid reviewId,
        Guid reporterUserId,
        IReadOnlyCollection<string> reasons,
        string? otherDetails)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review ID is required.", nameof(reviewId));
        if (reporterUserId == Guid.Empty)
            throw new ArgumentException("Reporter ID is required.", nameof(reporterUserId));
        if (reasons.Count == 0)
            throw new ArgumentException("At least one report reason is required.", nameof(reasons));

        return new ProductReviewReport
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            ReporterUserId = reporterUserId,
            ReasonsJson = System.Text.Json.JsonSerializer.Serialize(reasons),
            OtherDetails = string.IsNullOrWhiteSpace(otherDetails) ? null : otherDetails.Trim(),
            Status = ProductReviewReportStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Resolve(bool hideReview, Guid moderatorUserId)
    {
        if (Status != ProductReviewReportStatus.Pending)
            throw new InvalidOperationException("Only pending reports can be resolved.");
        if (moderatorUserId == Guid.Empty)
            throw new ArgumentException("Moderator ID is required.", nameof(moderatorUserId));

        Status = hideReview ? ProductReviewReportStatus.Hidden : ProductReviewReportStatus.Dismissed;
        ModeratorUserId = moderatorUserId;
        ResolvedAtUtc = DateTime.UtcNow;
    }

    public void MarkHidden(Guid moderatorUserId)
    {
        if (Status != ProductReviewReportStatus.Pending)
            return;
        Resolve(hideReview: true, moderatorUserId);
    }
}
