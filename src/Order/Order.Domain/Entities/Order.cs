using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Order.Domain.Enums;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public string OrderNumber { get; private set; } = "";
        public List<OrderItem> Items { get; private set; } = new List<OrderItem>();
        public Address DeliveryAddress { get; private set; }
        public OrderStatus Status { get; private set; }
        public decimal SubTotal => Items.Sum(x => x.TotalCost);
        public decimal DeliveryCost {  get; private set; }
        public decimal TotalCost => DeliveryCost + SubTotal;
        public PaymentType PaymentType { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Order() { }

        public static Order Create(
           Guid customerId,
           List<OrderItem> items,
           Address deliveryAddress,
           PaymentType paymentType,
           decimal deliveryCost,
           Guid? orderId = null)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer ID is required", nameof(customerId));

            if (items == null || !items.Any())
                throw new ArgumentException("Order must have at least one item", nameof(items));

            if (deliveryAddress == null)
                throw new ArgumentNullException(nameof(deliveryAddress));

            if (deliveryCost < 0)
                throw new ArgumentOutOfRangeException(nameof(deliveryCost), "Delivery cost cannot be negative");
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

            var order = new Order
            {
                Id = orderId ?? Guid.NewGuid(),
                CustomerId = customerId,
                OrderNumber = GenerateOrderNumber(),
                DeliveryAddress = deliveryAddress,
                Status = OrderStatus.Pending,
                DeliveryCost = deliveryCost,
                PaymentType = paymentType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                Items = items
            };

            foreach (var item in items)
            {
                item.OrderId = order.Id;
            }

            return order;
        }

        private static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        public void MarkAsPaid()
        {
            if (Status is not (OrderStatus.Pending or OrderStatus.PaymentConfirmationPending))
                throw new InvalidOperationException($"Cannot mark as paid because status is {Status}");

            Status = OrderStatus.Paid;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkPaymentConfirmationPending()
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot queue payment confirmation because status is {Status}");

            Status = OrderStatus.PaymentConfirmationPending;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsProcessing()
        {
            if (Status != OrderStatus.Paid)
                throw new InvalidOperationException($"Cannot process because status is {Status}");

            Status = OrderStatus.Processing;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsShipped()
        {
            if (Status != OrderStatus.Processing)
                throw new InvalidOperationException($"Cannot ship because status is {Status}");

            Status = OrderStatus.Shipped;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsDelivered()
        {
            if (Status != OrderStatus.Shipped)
                throw new InvalidOperationException($"Cannot deliver because status is {Status}");

            Status = OrderStatus.Delivered;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDeliveryAddress(Address address)
        {
            ArgumentNullException.ThrowIfNull(address);
            if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.PaymentConfirmationPending)
                throw new InvalidOperationException($"Cannot change delivery address because status is {Status}");

            DeliveryAddress = address;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.PaymentConfirmationPending)
                throw new InvalidOperationException($"Cannot cancel because status is {Status}");

            Status = OrderStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

    }
}
