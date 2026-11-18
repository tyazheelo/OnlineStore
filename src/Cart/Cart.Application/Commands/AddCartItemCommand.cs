using System;

namespace Cart.Application.Commands
{
    public class AddCartItemCommand
    {
        public Guid CustomerId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
