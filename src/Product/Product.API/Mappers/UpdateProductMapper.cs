using Product.Application.Commands;
using Shop.Contracts.Requests.Product;
using System;

namespace Product.API.Mappers
{
    public static class UpdateProductMapper
    {
        public static UpdateProductCommand ToCommand(UpdateProductRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new UpdateProductCommand
            {
                Name = request.Name ?? string.Empty,
                NameEn = request.NameEn ?? string.Empty,
                Description = request.Description ?? string.Empty,
                DescriptionEn = request.DescriptionEn ?? string.Empty,
                ImageUrl = request.ImageUrl,
                Specifications = request.Specifications ?? new Dictionary<string, string>(),
                SpecificationsEn = request.SpecificationsEn?.ToDictionary(
                    item => item.Key,
                    item => new Product.Domain.Entities.ProductSpecificationTranslation(
                        item.Value?.Name ?? string.Empty,
                        item.Value?.Value ?? string.Empty))
                    ?? new Dictionary<string, Product.Domain.Entities.ProductSpecificationTranslation>(),
                Tags = request.Tags ?? new List<string>(),
                CategoryId = request.CategoryId,
                Price = request.Price
            };
        }
    }
}