using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Contracts.DTOs.Product
{
    public class ProductDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Dictionary<string, string> Specifications { get; set; } = new();
        public Dictionary<string, ProductSpecificationTranslationDTO> SpecificationsEn { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryNameEn { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public decimal RatingAverage { get; set; }
        public int ReviewCount { get; set; }
    }
}
