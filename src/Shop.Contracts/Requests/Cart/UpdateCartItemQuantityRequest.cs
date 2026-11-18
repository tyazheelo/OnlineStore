using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Cart;

public sealed class UpdateCartItemQuantityRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
