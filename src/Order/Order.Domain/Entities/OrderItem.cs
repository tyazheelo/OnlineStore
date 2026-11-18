using System;

namespace Order.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; private set; }
        public Guid OrderId { get; internal set; }
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public decimal Price { get; private set; }
        public decimal TotalCost => Price * Quantity;

        private OrderItem() { }

        public static OrderItem Create(
            Guid orderId,
            Guid productId,
            string productName,
            int quantity,
            decimal price,
            bool isConnectedToOrder)
        {
            if (orderId == Guid.Empty && isConnectedToOrder)
                throw new ArgumentException("Order ID is required", nameof(orderId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Product ID is required", nameof(productId));

            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name is required", nameof(productName));

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");

            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative");

            return new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                Price = price
            };
        }
    }
}