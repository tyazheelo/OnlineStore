using Shop.Contracts.DTOs.Identity;
using Shop.Contracts.Requests.Identity;

namespace Identity.Application.Interfaces.Services;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<List<CustomerSummaryDto>> GetProfilesAsync(IReadOnlyCollection<Guid> userIds);
    Task<(UserProfileDto? Profile, IReadOnlyList<string> Errors)> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request);
    Task<List<SavedAddressDto>> GetAddressesAsync(Guid userId);
    Task<SavedAddressDto> AddAddressAsync(Guid userId, SaveUserAddressRequest request);
    Task<SavedAddressDto?> UpdateAddressAsync(Guid userId, Guid addressId, SaveUserAddressRequest request);
    Task<bool> DeleteAddressAsync(Guid userId, Guid addressId);
}
