using Order.Domain.Entities;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.Domain.Tests;

public sealed class OrderPaymentConfirmationTests
{
    [Fact]
    public void OrderMovesThroughConfirmationBeforePaid()
    {
        var order = CreatePendingOrder();

        order.MarkPaymentConfirmationPending();
        Assert.Equal(OrderStatus.PaymentConfirmationPending, order.Status);

        order.MarkAsPaid();
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void CancelledOrderCannotQueuePaymentConfirmation()
    {
        var order = CreatePendingOrder();
        order.Cancel();

        Assert.Throws<InvalidOperationException>(order.MarkPaymentConfirmationPending);
    }

    private static Order.Domain.Entities.Order CreatePendingOrder()
    {
        var item = OrderItem.Create(
            Guid.Empty,
            Guid.NewGuid(),
            "Test item",
            quantity: 1,
            price: 10m,
            isConnectedToOrder: false);

        return Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            new List<OrderItem> { item },
            new Address("Country", "", "City", "Street", "12345"),
            PaymentType.ByCard,
            deliveryCost: 0m);
    }
}
