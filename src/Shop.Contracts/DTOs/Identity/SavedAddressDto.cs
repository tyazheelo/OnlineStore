namespace Shop.Contracts.DTOs.Identity;

public sealed class SavedAddressDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? State { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
