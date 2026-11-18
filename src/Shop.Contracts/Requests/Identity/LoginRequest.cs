using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Identity;

public class LoginRequest
{
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
