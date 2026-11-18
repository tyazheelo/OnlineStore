namespace Product.Application.Commands;

public sealed class ReserveStockCommand
{
    public Guid OrderId { get; set; }
    public List<ReserveStockItemCommand> Items { get; set; } = [];
}

public sealed class ReserveStockItemCommand
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
