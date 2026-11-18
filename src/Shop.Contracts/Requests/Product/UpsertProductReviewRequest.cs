using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Product;

public sealed class UpsertProductReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }
}
