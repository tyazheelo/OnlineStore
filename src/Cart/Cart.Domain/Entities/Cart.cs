using System;
using System.Collections.Generic;
using System.Linq;

namespace Cart.Domain.Entities
{
    public class Cart
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public long Revision { get; private set; }
        public List<CartItem> Items { get; private set; } = new();
        public decimal EstimatedTotalCost => Items.Sum(x => x.LineTotalSnapshot);

        private Cart() { }

        public static Cart Create(Guid customerId)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer ID is required.", nameof(customerId));

            return new Cart()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Revision = 0,
                Items = new List<CartItem>()
            };
        }

        public void Clear()
        {
            if (Items.Count == 0)
                return;

            Items = new List<CartItem>();
            IncrementRevision();
        }

        public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
        {
            var item = Items.Find(x => x.ProductId == productId);
            if (item != null)
            {
                item.UpdateProductSnapshot(productName, unitPrice);
                item.IncreaseQuantity(quantity);
            }
            else
            {
                Items.Add(CartItem.Create(productId, productName, quantity, unitPrice));
            }

            IncrementRevision();
        }

        public bool SetItemQuantity(Guid productId, string productName, decimal unitPrice, int quantity)
        {
            var item = Items.Find(x => x.ProductId == productId);
            if (item == null)
                return false;

            item.SetQuantity(quantity);
            item.UpdateProductSnapshot(productName, unitPrice);
            IncrementRevision();
            return true;
        }

        public bool RemoveItem(Guid productId)
        {
            var item = Items.Find(x => x.ProductId == productId);
            if (item == null || !Items.Remove(item))
                return false;

            IncrementRevision();
            return true;
        }

        private void IncrementRevision() => Revision = checked(Revision + 1);
    }
}
