using System.Net;
using System.Net.Http.Json;
using Cart.Application.Interfaces.Services;
using Cart.Application.Models;
using Shop.Contracts.DTOs.Product;

namespace Cart.Infrastructure.Clients;

public sealed class ProductCatalogClient : IProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public ProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatalogProduct?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/product/{productId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDTO>(cancellationToken: cancellationToken);
        return product is null
            ? null
            : new CatalogProduct(product.Id, product.Name, product.Price, product.StockQuantity, product.IsActive);
    }

    public async Task<IReadOnlyList<CatalogProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var products = new List<CatalogProduct>();
        foreach (var batch in productIds.Distinct().Chunk(100))
        {
            var query = string.Join("&", batch.Select(id => $"ids={Uri.EscapeDataString(id.ToString())}"));
            using var response = await _httpClient.GetAsync($"api/product/by-ids?{query}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var batchProducts = await response.Content.ReadFromJsonAsync<List<ProductDTO>>(cancellationToken: cancellationToken);
            if (batchProducts is not null)
            {
                products.AddRange(batchProducts.Select(product =>
                    new CatalogProduct(product.Id, product.Name, product.Price, product.StockQuantity, product.IsActive)));
            }
        }

        return products;
    }
}
