using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Identity;

public sealed class UpdateUserProfileRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9._+-]+$")]
    public string UserName { get; set; } = string.Empty;

    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; set; }
}
