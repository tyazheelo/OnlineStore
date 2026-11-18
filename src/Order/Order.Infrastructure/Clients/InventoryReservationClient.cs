using System.Net;
using System.Net.Http.Json;
using Order.Application.Commands;
using Order.Application.Interfaces.Services;
using Shop.Contracts.Requests.Product;

namespace Order.Infrastructure.Clients;

public sealed class InventoryReservationClient : IInventoryReservationClient
{
    private readonly HttpClient _httpClient;

    public InventoryReservationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ReserveAsync(
        Guid orderId,
        IReadOnlyCollection<OrderItemCommand> items,
        CancellationToken cancellationToken = default)
    {
        var request = new ReserveStockRequest
        {
            OrderId = orderId,
            Items = items.Select(item => new ReserveStockItemRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync("api/inventory/reservations", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ConfirmAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            $"api/inventory/reservations/{orderId}/confirm", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ReleaseAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            $"api/inventory/reservations/{orderId}/release", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<bool> ExpireAsync(
        Guid orderId,
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        var cutoff = Uri.EscapeDataString(cutoffUtc.ToString("O"));
        using var response = await _httpClient.PostAsync(
            $"api/inventory/reservations/{orderId}/expire?cutoffUtc={cutoff}", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
    }

    public async Task<bool> IsConfirmedAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/inventory/reservations/{orderId}/confirmed", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync(cancellationToken));
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException(await response.Content.ReadAsStringAsync(cancellationToken));

        response.EnsureSuccessStatusCode();
    }
}
