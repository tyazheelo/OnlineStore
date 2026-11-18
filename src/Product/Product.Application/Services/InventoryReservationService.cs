using Product.Application.Commands;
using Product.Application.Interfaces.Repositories;
using Product.Application.Interfaces.Services;
using Product.Domain.Entities;

namespace Product.Application.Services;

public sealed class InventoryReservationService : IInventoryReservationService
{
    private readonly IProductRepository _repository;

    public InventoryReservationService(IProductRepository repository)
    {
        _repository = repository;
    }

    public Task ReserveAsync(ReserveStockCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var reservation = StockReservation.Create(
            command.OrderId,
            command.Items.Select(item => (item.ProductId, item.Quantity)));

        return _repository.ReserveStockAsync(reservation, cancellationToken);
    }

    public Task ConfirmAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        return _repository.ConfirmReservationAsync(orderId, cancellationToken);
    }

    public Task ReleaseAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        return _repository.ReleaseReservationAsync(orderId, cancellationToken);
    }

    public Task<bool> ExpireAsync(Guid orderId, DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        if (cutoffUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The cutoff must be UTC.", nameof(cutoffUtc));

        return _repository.ExpireReservationAsync(orderId, cutoffUtc, cancellationToken);
    }

    public Task<bool> IsConfirmedAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        return _repository.IsReservationConfirmedAsync(orderId, cancellationToken);
    }
}
