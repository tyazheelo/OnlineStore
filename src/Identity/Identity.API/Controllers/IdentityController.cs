using Identity.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shop.Contracts.Requests.Identity;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public IdentityController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var (response, errors) = await _identityService.RegisterAsync(request);
        return response is null ? BadRequest(new { errors }) : Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await _identityService.LoginAsync(request);
        return response is null ? Unauthorized() : Ok(response);
    }
}
