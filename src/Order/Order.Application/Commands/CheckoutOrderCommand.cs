using Order.Domain.Enums;
using Order.Domain.ValueObjects;

namespace Order.Application.Commands;

public sealed class CheckoutOrderCommand
{
    public Guid CustomerId { get; set; }
    public Address DeliveryAddress { get; set; } = null!;
    public PaymentType PaymentType { get; set; }
}
