using Identity.Application.Interfaces.Repositories;
using Identity.Application.Interfaces.Services;
using Identity.Domain.Entities;
using Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Contracts.DTOs.Identity;
using Shop.Contracts.Requests.Identity;

namespace Identity.Infrastructure.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAddressRepository _addressRepository;

    public UserProfileService(
        UserManager<ApplicationUser> userManager,
        IUserAddressRepository addressRepository)
    {
        _userManager = userManager;
        _addressRepository = addressRepository;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToProfileDto(user);
    }

    public async Task<List<CustomerSummaryDto>> GetProfilesAsync(IReadOnlyCollection<Guid> userIds)
    {
        return await _userManager.Users
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new CustomerSummaryDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName
            })
            .ToListAsync();
    }

    public async Task<(UserProfileDto? Profile, IReadOnlyList<string> Errors)> UpdateProfileAsync(
        Guid userId,
        UpdateUserProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (null, new[] { "User not found." });

        user.UserName = request.UserName.Trim();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? (ToProfileDto(user), Array.Empty<string>())
            : (null, result.Errors.Select(error => error.Code == "DuplicateUserName"
                ? "This username is already taken."
                : error.Description).ToArray());
    }

    public async Task<List<SavedAddressDto>> GetAddressesAsync(Guid userId)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        return addresses.Select(ToAddressDto).ToList();
    }

    public async Task<SavedAddressDto> AddAddressAsync(Guid userId, SaveUserAddressRequest request)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        var makeDefault = request.IsDefault || addresses.Count == 0;
        if (makeDefault)
        {
            foreach (var address in addresses)
                address.SetDefault(false);
        }

        var newAddress = SavedAddress.Create(
            userId,
            request.Label,
            request.Country,
            request.State,
            request.City,
            request.Street,
            request.PostalCode,
            makeDefault);

        await _addressRepository.AddAsync(newAddress);
        await _addressRepository.SaveChangesAsync();
        return ToAddressDto(newAddress);
    }

    public async Task<SavedAddressDto?> UpdateAddressAsync(
        Guid userId,
        Guid addressId,
        SaveUserAddressRequest request)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        var address = addresses.FirstOrDefault(item => item.Id == addressId);
        if (address is null)
            return null;

        var makeDefault = request.IsDefault
            || address.IsDefault
            || addresses.All(item => item.Id == addressId || !item.IsDefault);

        if (makeDefault)
        {
            foreach (var item in addresses)
                item.SetDefault(item.Id == addressId);
        }

        address.Update(request.Label, request.Country, request.State, request.City, request.Street, request.PostalCode);
        address.SetDefault(makeDefault);
        await _addressRepository.SaveChangesAsync();
        return ToAddressDto(address);
    }

    public async Task<bool> DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        var address = addresses.FirstOrDefault(item => item.Id == addressId);
        if (address is null)
            return false;

        _addressRepository.Remove(address);
        if (address.IsDefault)
            addresses.FirstOrDefault(item => item.Id != addressId)?.SetDefault(true);

        await _addressRepository.SaveChangesAsync();
        return true;
    }

    private static UserProfileDto ToProfileDto(ApplicationUser user)
    {
        return new UserProfileDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber
        };
    }

    private static SavedAddressDto ToAddressDto(SavedAddress address)
    {
        return new SavedAddressDto
        {
            Id = address.Id,
            Label = address.Label,
            Country = address.Country,
            State = address.State,
            City = address.City,
            Street = address.Street,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        };
    }
}
