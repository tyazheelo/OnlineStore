using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Cart
{
    public class CartItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}