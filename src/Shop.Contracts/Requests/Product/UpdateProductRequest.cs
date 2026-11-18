using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shop.Contracts.DTOs.Product;

namespace Shop.Contracts.Requests.Product
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Dictionary<string, string> Specifications { get; set; } = new();
        public Dictionary<string, ProductSpecificationTranslationDTO> SpecificationsEn { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public Guid CategoryId { get; set; }
        public decimal Price { get; set; }
    }
}
