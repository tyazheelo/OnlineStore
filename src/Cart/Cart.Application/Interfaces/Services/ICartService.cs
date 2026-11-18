using System;
using System.Threading;
using System.Threading.Tasks;
using Cart.Application.Commands;
using Cart.Domain.Entities;

using CartEntity = Cart.Domain.Entities.Cart;
namespace Cart.Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<CartEntity?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<CartEntity> GetOrCreateCartForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<CartEntity> GetCurrentCartAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task AddItemAsync(AddCartItemCommand command, CancellationToken cancellationToken = default);
        Task<bool> UpdateItemQuantityAsync(UpdateCartItemQuantityCommand command, CancellationToken cancellationToken = default);
        Task<bool> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
        Task<bool> ClearCartAsync(
            Guid customerId,
            long? expectedRevision = null,
            CancellationToken cancellationToken = default);
    }
}