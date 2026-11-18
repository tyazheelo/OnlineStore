using Product.Domain.Enums;

namespace Product.Domain.Entities;

public sealed class StockReservation
{
    public Guid OrderId { get; private set; }
    public StockReservationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public List<StockReservationItem> Items { get; private set; } = [];

    private StockReservation() { }

    public static StockReservation Create(Guid orderId, IEnumerable<(Guid ProductId, int Quantity)> items)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));

        var requestedItems = items.ToList();
        if (requestedItems.Any(item => item.ProductId == Guid.Empty || item.Quantity <= 0))
            throw new ArgumentException("Each reservation item must have a product ID and positive quantity.", nameof(items));

        var normalizedItems = requestedItems
            .GroupBy(item => item.ProductId)
            .Select(group => (ProductId: group.Key, Quantity: group.Sum(item => item.Quantity)))
            .ToList();

        if (normalizedItems.Count == 0)
            throw new ArgumentException("At least one reservation item is required.", nameof(items));

        return new StockReservation
        {
            OrderId = orderId,
            Status = StockReservationStatus.Reserved,
            CreatedAtUtc = DateTime.UtcNow,
            Items = normalizedItems
                .Select(item => StockReservationItem.Create(orderId, item.ProductId, item.Quantity))
                .ToList()
        };
    }

    public bool Matches(IEnumerable<(Guid ProductId, int Quantity)> items)
    {
        var requested = items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        return requested.Count == Items.Count
            && Items.All(item => requested.TryGetValue(item.ProductId, out var quantity) && quantity == item.Quantity);
    }

    public void Confirm()
    {
        if (Status == StockReservationStatus.Released)
            throw new InvalidOperationException("A released reservation cannot be confirmed.");
        Status = StockReservationStatus.Confirmed;
    }

    public void Release()
    {
        Status = StockReservationStatus.Released;
    }
}
