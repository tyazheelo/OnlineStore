using Order.Application.Models;

namespace Order.Application.Interfaces.Services;

public interface IProductCatalogClient
{
    Task<IReadOnlyList<CatalogProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}
