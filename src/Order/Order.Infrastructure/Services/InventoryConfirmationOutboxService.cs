using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Application.Interfaces.Repositories;
using Order.Application.Interfaces.Services;

namespace Order.Infrastructure.Services;

public sealed class InventoryConfirmationOutboxService : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryConfirmationOutboxService> _logger;

    public InventoryConfirmationOutboxService(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryConfirmationOutboxService> logger)
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
                await ProcessDueConfirmationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process inventory confirmation outbox.");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task ProcessDueConfirmationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var inventoryClient = scope.ServiceProvider.GetRequiredService<IInventoryReservationClient>();
        var orderIds = await repository.GetDueInventoryConfirmationIdsAsync(DateTime.UtcNow, BatchSize);

        foreach (var orderId in orderIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await inventoryClient.ConfirmAsync(orderId, cancellationToken);
                await repository.CompleteInventoryConfirmationAsync(orderId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                try
                {
                    await repository.RecordInventoryConfirmationFailureAsync(orderId, DateTime.UtcNow);
                }
                catch (Exception recordingException)
                {
                    _logger.LogError(
                        new AggregateException(exception, recordingException),
                        "Could not reschedule inventory confirmation for order {OrderId}.",
                        orderId);
                    continue;
                }

                _logger.LogWarning(exception, "Inventory confirmation for order {OrderId} will be retried.", orderId);
            }
        }
    }
}
