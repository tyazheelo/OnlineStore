using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Order
{
    public class CreateOrderRequest
    {
        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string PaymentType { get; set; } = string.Empty;
    }
}
