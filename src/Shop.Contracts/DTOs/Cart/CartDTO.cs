using System;
using System.Collections.Generic;

namespace Shop.Contracts.DTOs.Cart
{
    public class CartDTO
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public long Revision { get; set; }
        public List<CartItemDTO> Items { get; set; } = new List<CartItemDTO>();
        public decimal EstimatedTotalCost { get; set; }
    }
}