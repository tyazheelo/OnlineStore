using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Product;

public class CreateCategoryRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string NameEn { get; set; } = string.Empty;
}
