using System.Net.Http.Json;
using Order.Application.Interfaces.Services;
using Order.Application.Models;
using Shop.Contracts.DTOs.Product;

namespace Order.Infrastructure.Clients;

public sealed class ProductCatalogClient : IProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public ProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
