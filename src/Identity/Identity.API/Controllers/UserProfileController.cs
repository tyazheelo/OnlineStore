using System.Security.Claims;
using Identity.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Contracts.Requests.Identity;

namespace Identity.API.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/me")]
public sealed class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _profileService;

    public UserProfileController(IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var profile = await _profileService.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (profile, errors) = await _profileService.UpdateProfileAsync(userId, request);
        return profile is null ? BadRequest(new { errors }) : Ok(profile);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _profileService.GetAddressesAsync(userId));
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(SaveUserAddressRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _profileService.AddAddressAsync(userId, request));
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid addressId, SaveUserAddressRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var address = await _profileService.UpdateAddressAsync(userId, addressId, request);
        return address is null ? NotFound() : Ok(address);
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return await _profileService.DeleteAddressAsync(userId, addressId) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
