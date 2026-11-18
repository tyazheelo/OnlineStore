using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Identity;

public sealed class UserLookupRequest
{
    [Required, MinLength(1), MaxLength(500)]
    public List<Guid> UserIds { get; set; } = [];
}
