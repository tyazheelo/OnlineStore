using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Product.Application.Interfaces.Repositories;
using Shop.Contracts;

namespace Product.Infrastructure.Services;

public sealed class ExpiredStockReservationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredStockReservationService> _logger;

    public ExpiredStockReservationService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredStockReservationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to scan for expired stock reservations.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(InventoryReservationPolicy.ScanIntervalMinutes),
                stoppingToken);
        }
    }

    private async Task ExpireReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var cutoffUtc = DateTime.UtcNow - TimeSpan.FromMinutes(InventoryReservationPolicy.LifetimeMinutes);
        var orderIds = await repository.GetExpiredReservationOrderIdsAsync(cutoffUtc, cancellationToken);

        foreach (var orderId in orderIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await repository.ExpireReservationAsync(orderId, cutoffUtc, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to release expired stock reservation for order {OrderId}.", orderId);
            }
        }
    }
}
