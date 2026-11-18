using Shop.Contracts.DTOs.Identity;
using Shop.Contracts.Requests.Identity;

namespace Identity.Application.Interfaces.Services;

public interface IIdentityService
{
    Task<(AuthResponse? Response, IReadOnlyList<string> Errors)> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
