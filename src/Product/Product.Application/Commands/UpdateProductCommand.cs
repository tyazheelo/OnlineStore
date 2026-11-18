using Product.Domain.Entities;
using System;

namespace Product.Application.Commands
{
    public class UpdateProductCommand
    {
        public string Name { get; init; } = string.Empty;
        public string NameEn { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string DescriptionEn { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public Dictionary<string, string> Specifications { get; init; } = new();
        public Dictionary<string, ProductSpecificationTranslation> SpecificationsEn { get; init; } = new();
        public List<string> Tags { get; init; } = [];
        public Guid CategoryId { get; init; }
        public decimal Price { get; init; }
    }
}