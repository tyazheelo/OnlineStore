namespace Shop.Contracts.DTOs.Order;

public sealed class DeliveryAddressDTO
{
    public string Country { get; set; } = string.Empty;
    public string? State { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}
