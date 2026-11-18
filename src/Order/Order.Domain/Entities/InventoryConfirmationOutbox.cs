namespace Order.Domain.Entities;

public sealed class InventoryConfirmationOutbox
{
    private const int MaximumRetryDelayMinutes = 15;

    public Guid OrderId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public int AttemptCount { get; private set; }

    private InventoryConfirmationOutbox()
    {
    }

    public static InventoryConfirmationOutbox Create(Guid orderId, DateTime nowUtc)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The timestamp must be UTC.", nameof(nowUtc));

        return new InventoryConfirmationOutbox
        {
            OrderId = orderId,
            CreatedAtUtc = nowUtc,
            NextAttemptAtUtc = nowUtc
        };
    }

    public void RecordFailure(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The timestamp must be UTC.", nameof(nowUtc));

        AttemptCount++;
        var delayMinutes = Math.Min(1 << Math.Min(AttemptCount - 1, 10), MaximumRetryDelayMinutes);
        NextAttemptAtUtc = nowUtc.AddMinutes(delayMinutes);
    }
}
