using System;
using System.Collections.Generic;

namespace Shop.Contracts.Requests.Cart
{
    public class CartRequest
    {
        public Guid CustomerId { get; set; }
        public List<CartItemRequest> Items { get; set; } = new List<CartItemRequest>();
    }
}