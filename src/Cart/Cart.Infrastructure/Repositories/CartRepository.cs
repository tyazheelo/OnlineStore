using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cart.Application.Exceptions;
using Cart.Infrastructure.Data;
using Cart.Application.Interfaces.Repositories;

using CartEntity = Cart.Domain.Entities.Cart;

namespace Cart.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly CartDbContext _db;

        public CartRepository(CartDbContext db)
        {
            _db = db;
        }

        public async Task<CartEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<CartEntity?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        }

        public async Task AddAsync(CartEntity cart, CancellationToken cancellationToken = default)
        {
            await _db.Carts.AddAsync(cart, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(CartEntity cart, CancellationToken cancellationToken = default)
        {
            foreach (var item in cart.Items)
            {
                if (_db.Entry(item).State == EntityState.Detached)
                    _db.CartItems.Add(item);
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _db.ChangeTracker.Clear();
                throw new CartConcurrencyException(exception);
            }
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Carts.FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                _db.Carts.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
