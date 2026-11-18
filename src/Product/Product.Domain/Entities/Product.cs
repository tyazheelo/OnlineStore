using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string NameEn { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string DescriptionEn { get; private set; } = string.Empty;
        public string? ImageUrl { get; private set; }
        public Dictionary<string, string> Specifications { get; private set; } = new();
        public Dictionary<string, ProductSpecificationTranslation> SpecificationsEn { get; private set; } = new();
        public List<string> Tags { get; private set; } = [];
        public Guid CategoryId { get; private set; }
        public Category Category { get; private set; } = null!;
        public decimal Price { get; private set; }
        public int StockQuantity { get; private set; }
        public bool IsActive { get; private set; }

        private Product() { }

        public static Product Create(
            string name,
            string description,
            Category category,
            decimal price,
            int stockQuantity,
            string? imageUrl = null,
            IReadOnlyDictionary<string, string>? specifications = null,
            IReadOnlyCollection<string>? tags = null,
            string? nameEn = null,
            string? descriptionEn = null,
            IReadOnlyDictionary<string, ProductSpecificationTranslation>? specificationsEn = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name can't be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description can't be empty", nameof(description));
            ArgumentNullException.ThrowIfNull(category);
            if (category.Id == Guid.Empty) throw new ArgumentException("Category is required", nameof(category));
            if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Price should be greater than zero");
            if (stockQuantity < 0) throw new ArgumentOutOfRangeException("Quantities can't be negative");
            ValidateEnglishText(nameEn, 200, nameof(nameEn));
            ValidateEnglishText(descriptionEn, 1000, nameof(descriptionEn));
            ValidateImageUrl(imageUrl);

            var copiedSpecifications = CopySpecifications(specifications);

            return new Product()
            {
                Id = Guid.NewGuid(),
                Name = name,
                NameEn = nameEn?.Trim() ?? string.Empty,
                Description = description,
                DescriptionEn = descriptionEn?.Trim() ?? string.Empty,
                ImageUrl = imageUrl,
                Specifications = copiedSpecifications,
                SpecificationsEn = CopySpecificationTranslations(specificationsEn, copiedSpecifications),
                Tags = CopyTags(tags),
                CategoryId = category.Id,
                Category = category,
                Price = price,
                StockQuantity = stockQuantity,
                IsActive = true
            };
        }

        public void UpdateStock(int quantityChange)
        {
            if (StockQuantity + quantityChange < 0) throw new ArgumentException("Not enough stock to remove this quantity");
            StockQuantity += quantityChange;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        // Новый метод для обновления основных полей продукта
        public void Update(
            string name,
            string description,
            Category category,
            decimal price,
            string? imageUrl,
            IReadOnlyDictionary<string, string>? specifications,
            IReadOnlyCollection<string>? tags,
            string? nameEn = null,
            string? descriptionEn = null,
            IReadOnlyDictionary<string, ProductSpecificationTranslation>? specificationsEn = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name), "Name can't be empty");
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description), "Description can't be empty");
            ArgumentNullException.ThrowIfNull(category);
            if (category.Id == Guid.Empty) throw new ArgumentException("Category is required", nameof(category));
            if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Price should be greater than zero");
            ValidateEnglishText(nameEn, 200, nameof(nameEn));
            ValidateEnglishText(descriptionEn, 1000, nameof(descriptionEn));
            ValidateImageUrl(imageUrl);

            var specificationsCopy = CopySpecifications(specifications);
            var translationsCopy = CopySpecificationTranslations(specificationsEn, specificationsCopy);
            var tagsCopy = CopyTags(tags);
            Name = name;
            NameEn = nameEn?.Trim() ?? string.Empty;
            Description = description;
            DescriptionEn = descriptionEn?.Trim() ?? string.Empty;
            ImageUrl = imageUrl;
            Specifications = specificationsCopy;
            SpecificationsEn = translationsCopy;
            Tags = tagsCopy;
            CategoryId = category.Id;
            Category = category;
            Price = price;
        }

        private static Dictionary<string, string> CopySpecifications(
            IReadOnlyDictionary<string, string>? specifications)
        {
            if (specifications == null)
                return new Dictionary<string, string>();

            if (specifications.Any(item =>
                    string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
                throw new ArgumentException(
                    "Specification names and values cannot be empty.",
                    nameof(specifications));

            return specifications.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.Ordinal);
        }

        private static Dictionary<string, ProductSpecificationTranslation> CopySpecificationTranslations(
            IReadOnlyDictionary<string, ProductSpecificationTranslation>? translations,
            IReadOnlyDictionary<string, string> specifications)
        {
            if (translations == null)
                return new Dictionary<string, ProductSpecificationTranslation>(StringComparer.Ordinal);

            if (translations.Any(item =>
                    string.IsNullOrWhiteSpace(item.Key)
                    || !specifications.ContainsKey(item.Key)
                    || item.Value is null
                    || string.IsNullOrWhiteSpace(item.Value.Name)
                    || string.IsNullOrWhiteSpace(item.Value.Value)))
                throw new ArgumentException(
                    "English specification translations must reference existing specifications and include a name and value.",
                    nameof(translations));

            return translations.ToDictionary(
                item => item.Key,
                item => new ProductSpecificationTranslation(item.Value.Name.Trim(), item.Value.Value.Trim()),
                StringComparer.Ordinal);
        }

        private static void ValidateImageUrl(string? imageUrl)
        {
            if (imageUrl != null && string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Image URL cannot be blank.", nameof(imageUrl));
        }

        private static void ValidateEnglishText(string? value, int maxLength, string parameterName)
        {
            if (value is not null && value.Trim().Length > maxLength)
                throw new ArgumentException(
                    $"English text must be at most {maxLength} characters.",
                    parameterName);
        }

        private static List<string> CopyTags(IReadOnlyCollection<string>? tags)
        {
            if (tags == null)
                return [];

            if (tags.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Tags cannot be empty.", nameof(tags));

            return tags
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
