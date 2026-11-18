namespace Identity.Domain.Entities;

public sealed class SavedAddress
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    private SavedAddress()
    {
    }

    public static SavedAddress Create(
        Guid userId,
        string label,
        string country,
        string? state,
        string city,
        string street,
        string postalCode,
        bool isDefault)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        var address = new SavedAddress { UserId = userId };
        address.Update(label, country, state, city, street, postalCode);
        address.Id = Guid.NewGuid();
        address.IsDefault = isDefault;
        return address;
    }

    public void Update(
        string label,
        string country,
        string? state,
        string city,
        string street,
        string postalCode)
    {
        Label = Required(label, nameof(label));
        Country = Required(country, nameof(country));
        State = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        City = Required(city, nameof(city));
        Street = Required(street, nameof(street));
        PostalCode = Required(postalCode, nameof(postalCode));
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        return value.Trim();
    }
}
