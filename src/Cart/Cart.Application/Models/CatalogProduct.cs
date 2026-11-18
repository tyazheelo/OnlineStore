namespace Cart.Application.Models;

public sealed record CatalogProduct(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity,
    bool IsActive);
