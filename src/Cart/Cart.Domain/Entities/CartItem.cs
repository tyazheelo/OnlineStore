using System;

namespace Cart.Domain.Entities
{
    public class CartItem
    {
        public Guid Id { get; private set; }
        public Guid CartId { get; private set; }
        public Guid ProductId { get; private set; }
        public string ProductNameSnapshot { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public decimal UnitPriceSnapshot { get; private set; }
        public bool IsAvailable { get; private set; } = true;
        public decimal LineTotalSnapshot => UnitPriceSnapshot * Quantity;

        private CartItem() { }

        public static CartItem Create(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (productId == Guid.Empty) throw new ArgumentException("Product ID is required", nameof(productId));
            if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("Product name is required", nameof(productName));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity should be positive");
            if (unitPrice <= 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Price should be positive");

            return new CartItem()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductNameSnapshot = productName,
                Quantity = quantity,
                UnitPriceSnapshot = unitPrice
            };
        }

        public void IncreaseQuantity(int quantityChange)
        {
            if (quantityChange <= 0) throw new ArgumentOutOfRangeException(nameof(quantityChange), "QuantityChange should be positive");
            Quantity += quantityChange;
        }

        public void SetQuantity(int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity should be positive");
            Quantity = quantity;
        }

        public void DecreaseQuantity(int quantityChange)
        {
            if (quantityChange <= 0) throw new ArgumentOutOfRangeException(nameof(quantityChange), "QuantityChange should be positive");
            Quantity -= quantityChange;
        }
        public void UpdateProductSnapshot(string productName, decimal unitPrice, bool isAvailable = true)
        {
            if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("Product name is required", nameof(productName));
            if (unitPrice <= 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Price should be positive");
            ProductNameSnapshot = productName;
            UnitPriceSnapshot = unitPrice;
            IsAvailable = isAvailable;
        }

        public void SetAvailability(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
        internal void SetCartId(Guid cartId) => CartId = cartId;
    }
}
