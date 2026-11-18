using Order.Domain.Entities;
using Xunit;

namespace Order.Domain.Tests;

public sealed class InventoryConfirmationOutboxTests
{
    [Fact]
    public void CreateMakesTheConfirmationDueImmediately()
    {
        var nowUtc = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
        var outboxItem = InventoryConfirmationOutbox.Create(Guid.NewGuid(), nowUtc);

        Assert.Equal(nowUtc, outboxItem.CreatedAtUtc);
        Assert.Equal(nowUtc, outboxItem.NextAttemptAtUtc);
        Assert.Equal(0, outboxItem.AttemptCount);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    [InlineData(5, 15)]
    [InlineData(6, 15)]
    public void RecordFailureAppliesCappedExponentialBackoff(int failureCount, int expectedDelayMinutes)
    {
        var nowUtc = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
        var outboxItem = InventoryConfirmationOutbox.Create(Guid.NewGuid(), nowUtc);

        for (var attempt = 0; attempt < failureCount; attempt++)
            outboxItem.RecordFailure(nowUtc);

        Assert.Equal(failureCount, outboxItem.AttemptCount);
        Assert.Equal(nowUtc.AddMinutes(expectedDelayMinutes), outboxItem.NextAttemptAtUtc);
    }
}
