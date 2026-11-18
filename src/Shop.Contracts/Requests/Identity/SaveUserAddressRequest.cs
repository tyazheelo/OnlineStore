using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Identity;

public sealed class SaveUserAddressRequest
{
    [Required]
    [StringLength(50)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [StringLength(100)]
    public string? State { get; set; }

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
