using System;
using System.Threading;
using System.Threading.Tasks;
using Cart.Domain.Entities;

using CartEntity = Cart.Domain.Entities.Cart;

namespace Cart.Application.Interfaces.Repositories
{
    public interface ICartRepository
    {
        Task<CartEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CartEntity?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task AddAsync(CartEntity cart, CancellationToken cancellationToken = default);
        Task UpdateAsync(CartEntity cart, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
