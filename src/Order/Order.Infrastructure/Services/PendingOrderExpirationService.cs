using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Application.Interfaces.Repositories;
using Order.Application.Interfaces.Services;
using Shop.Contracts;

namespace Order.Infrastructure.Services;

public sealed class PendingOrderExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingOrderExpirationService> _logger;

    public PendingOrderExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingOrderExpirationService> logger)
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
                await ExpireOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to scan for expired pending orders.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(InventoryReservationPolicy.ScanIntervalMinutes),
                stoppingToken);
        }
    }

    private async Task ExpireOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var inventoryClient = scope.ServiceProvider.GetRequiredService<IInventoryReservationClient>();
        var cutoffUtc = DateTime.UtcNow - TimeSpan.FromMinutes(InventoryReservationPolicy.LifetimeMinutes);
        var orderIds = await orderRepository.GetPendingOrderIdsAsync();

        foreach (var orderId in orderIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await inventoryClient.IsConfirmedAsync(orderId, cancellationToken))
                {
                    await orderRepository.MarkPendingOrderPaidAsync(orderId);
                }
                else if (await inventoryClient.ExpireAsync(orderId, cutoffUtc, cancellationToken))
                {
                    await orderRepository.CancelPendingOrderAsync(orderId, cutoffUtc);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to expire reservation for pending order {OrderId}.", orderId);
            }
        }
    }
}
