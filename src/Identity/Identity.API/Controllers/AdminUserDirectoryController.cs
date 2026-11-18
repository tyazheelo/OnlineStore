using Identity.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Contracts.Requests.Identity;

namespace Identity.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/identity/admin/users")]
public sealed class AdminUserDirectoryController : ControllerBase
{
    private readonly IUserProfileService _profileService;

    public AdminUserDirectoryController(IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPost("lookup")]
    public async Task<IActionResult> Lookup([FromBody] UserLookupRequest request)
    {
        if (request.UserIds.Count == 0 || request.UserIds.Any(id => id == Guid.Empty))
            return BadRequest(new { error = "Provide one or more valid user IDs." });

        var profiles = await _profileService.GetProfilesAsync(request.UserIds.Distinct().ToArray());
        return Ok(profiles);
    }
}
