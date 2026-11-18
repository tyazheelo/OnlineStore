using Cart.Application.Models;

namespace Cart.Application.Interfaces.Services;

public interface IProductCatalogClient
{
    Task<CatalogProduct?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}
