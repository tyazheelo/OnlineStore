using System;

namespace Shop.Contracts.DTOs.Cart
{
    public class CartItemDTO
    {
        public Guid ProductId { get; set; }
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal LineTotalSnapshot { get; set; }
        public bool IsAvailable { get; set; }
    }
}