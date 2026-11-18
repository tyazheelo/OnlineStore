using Product.Application.Commands;

namespace Product.Application.Interfaces.Services;

public interface IInventoryReservationService
{
    Task ReserveAsync(ReserveStockCommand command, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> ExpireAsync(Guid orderId, DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<bool> IsConfirmedAsync(Guid orderId, CancellationToken cancellationToken = default);
}
