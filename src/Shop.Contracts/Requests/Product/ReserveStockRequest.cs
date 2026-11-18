using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Product;

public sealed class ReserveStockRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [MinLength(1)]
    public List<ReserveStockItemRequest> Items { get; set; } = [];
}

public sealed class ReserveStockItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
