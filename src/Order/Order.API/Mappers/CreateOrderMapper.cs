using Order.Application.Commands;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Shop.Contracts.Requests.Order;

namespace Order.API.Mappers
{
    public static class CreateOrderMapper
    {
        public static CheckoutOrderCommand ToCommand(CreateOrderRequest request, Guid customerId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!Enum.TryParse<PaymentType>(request.PaymentType, true, out var paymentType)
                || !Enum.IsDefined(paymentType))
                throw new ArgumentException("Invalid payment type", nameof(request.PaymentType));

            return new CheckoutOrderCommand
            {
                CustomerId = customerId,
                DeliveryAddress = new Address(
                    request.Country,
                    request.State ?? string.Empty,
                    request.City,
                    request.Street,
                    request.PostalCode
                ),
                PaymentType = paymentType
            };
        }
    }
}
