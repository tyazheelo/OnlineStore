namespace Product.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public List<Product> Products { get; private set; } = [];

    public static Category Create(string name, string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Укажите название категории.", nameof(name));

        var trimmedName = name.Trim();
        if (trimmedName.Length > 100)
            throw new ArgumentException("Название категории не должно быть длиннее 100 символов.", nameof(name));
        if (nameEn is not null && nameEn.Trim().Length > 100)
            throw new ArgumentException("English category name must be at most 100 characters.", nameof(nameEn));

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            NameEn = nameEn?.Trim() ?? string.Empty,
            NormalizedName = NormalizeName(trimmedName)
        };
    }

    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    private Category() { }
}
