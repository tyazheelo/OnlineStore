using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Order;

public sealed class AddSupportMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 2)]
    public string Message { get; set; } = string.Empty;
}
