using System.ComponentModel.DataAnnotations;

namespace Shop.Contracts.Requests.Order;

public sealed class CreateSupportTicketRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required, StringLength(2000, MinimumLength = 2)]
    public string Message { get; set; } = string.Empty;
}
