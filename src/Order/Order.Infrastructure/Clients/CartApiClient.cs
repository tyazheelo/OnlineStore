using System.Net.Http.Json;
using Order.Application.Interfaces.Services;
using Shop.Contracts.DTOs.Cart;

namespace Order.Infrastructure.Clients;

public sealed class CartApiClient : ICartClient
{
    private readonly HttpClient _httpClient;

    public CartApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CartDTO> GetCurrentCartAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/cart", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CartDTO>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Cart API returned an empty response.");
    }

    public async Task<bool> ClearCartAsync(
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/cart?revision={expectedRevision}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}
