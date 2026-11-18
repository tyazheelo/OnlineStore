using Shop.Contracts.DTOs.Identity;

namespace Order.Application.Interfaces.Services;

public interface ICustomerDirectoryClient
{
    Task<List<CustomerSummaryDto>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
}
