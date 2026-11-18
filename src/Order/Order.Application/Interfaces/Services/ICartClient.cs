using Shop.Contracts.DTOs.Cart;

namespace Order.Application.Interfaces.Services;

public interface ICartClient
{
    Task<CartDTO> GetCurrentCartAsync(CancellationToken cancellationToken = default);
    Task<bool> ClearCartAsync(long expectedRevision, CancellationToken cancellationToken = default);
}
