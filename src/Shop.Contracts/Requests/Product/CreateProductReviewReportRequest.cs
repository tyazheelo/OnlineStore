using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Product;

public sealed class CreateProductReviewReportRequest
{
    [Required]
    [MinLength(1)]
    public List<string> Reasons { get; set; } = [];

    [StringLength(1000)]
    public string? OtherDetails { get; set; }
}
