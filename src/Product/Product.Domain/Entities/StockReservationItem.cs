namespace Product.Domain.Entities;

public sealed class StockReservationItem
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private StockReservationItem() { }

    internal static StockReservationItem Create(Guid orderId, Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new StockReservationItem { OrderId = orderId, ProductId = productId, Quantity = quantity };
    }
}
