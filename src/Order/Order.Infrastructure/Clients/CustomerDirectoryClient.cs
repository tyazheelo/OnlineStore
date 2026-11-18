using System.Net.Http.Json;
using Shop.Contracts.DTOs.Identity;
using Shop.Contracts.Requests.Identity;
using Order.Application.Interfaces.Services;

namespace Order.Infrastructure.Clients;

public sealed class CustomerDirectoryClient : ICustomerDirectoryClient
{
    private readonly HttpClient _httpClient;

    public CustomerDirectoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CustomerSummaryDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var profiles = new List<CustomerSummaryDto>();
        foreach (var batch in userIds.Distinct().Chunk(500))
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/identity/admin/users/lookup",
                new UserLookupRequest { UserIds = batch.ToList() },
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var batchProfiles = await response.Content.ReadFromJsonAsync<List<CustomerSummaryDto>>(cancellationToken);
            if (batchProfiles is null)
                throw new HttpRequestException("Identity API returned an empty user lookup response.");
            profiles.AddRange(batchProfiles);
        }

        return profiles;
    }
}
