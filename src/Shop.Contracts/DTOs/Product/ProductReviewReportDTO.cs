namespace Shop.Contracts.DTOs.Product;

public sealed class ProductReviewReportDTO
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewCreatedAtUtc { get; set; }
    public Guid ReporterUserId { get; set; }
    public List<string> Reasons { get; set; } = [];
    public string? OtherDetails { get; set; }
    public DateTime ReportedAtUtc { get; set; }
}
