using Order.Application.Commands;
using Order.Domain.Enums;
using System;
using System.Linq;

namespace Order.Application.Validators
{
    public class CreateOrderValidator
    {
        public void Validate(CreateOrderCommand order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (order.CustomerId == Guid.Empty)
                throw new ArgumentException("Customer id is required", nameof(order.CustomerId));

            if (order.Items == null || !order.Items.Any())
                throw new ArgumentException("List of items can't be empty or null", nameof(order.Items));

            foreach (var item in order.Items)
            {
                if (item == null)
                    throw new ArgumentException("Order item cannot be null", nameof(order.Items));

                if (item.ProductId == Guid.Empty)
                    throw new ArgumentException("Product id is required", nameof(item.ProductId));

                if (string.IsNullOrWhiteSpace(item.ProductName))
                    throw new ArgumentException("Product name is required", nameof(item.ProductName));

                if (item.Quantity <= 0)
                    throw new ArgumentException("Product quantity must be greater than zero", nameof(item.Quantity));

                if (item.Price < 0)
                    throw new ArgumentException("Product price cannot be negative", nameof(item.Price));
            }

            if (order.DeliveryAddress == null)
                throw new ArgumentException("Delivery address is required", nameof(order.DeliveryAddress));

            if (string.IsNullOrWhiteSpace(order.DeliveryAddress.Country))
                throw new ArgumentException("Country is required", nameof(order.DeliveryAddress.Country));

            if (string.IsNullOrWhiteSpace(order.DeliveryAddress.City))
                throw new ArgumentException("City is required", nameof(order.DeliveryAddress.City));

            if (string.IsNullOrWhiteSpace(order.DeliveryAddress.Street))
                throw new ArgumentException("Street is required", nameof(order.DeliveryAddress.Street));

            if (string.IsNullOrWhiteSpace(order.DeliveryAddress.PostalCode) || order.DeliveryAddress.PostalCode.Length < 3)
                throw new ArgumentException("Postal code must be at least 3 characters", nameof(order.DeliveryAddress.PostalCode));

            // PaymentType is enum in command; still check it's defined
            if (!Enum.IsDefined(typeof(PaymentType), order.PaymentType))
                throw new ArgumentException("Invalid payment type", nameof(order.PaymentType));
        }
    }
}