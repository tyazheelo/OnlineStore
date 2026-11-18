using Order.Application.Commands;

namespace Order.Application.Interfaces.Services;

public interface IInventoryReservationClient
{
    Task ReserveAsync(Guid orderId, IReadOnlyCollection<OrderItemCommand> items, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> ExpireAsync(Guid orderId, DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<bool> IsConfirmedAsync(Guid orderId, CancellationToken cancellationToken = default);
}
