namespace Cart.Application.Commands;

public sealed class UpdateCartItemQuantityCommand
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
