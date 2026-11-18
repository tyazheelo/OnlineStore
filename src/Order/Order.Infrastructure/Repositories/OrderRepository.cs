using Order.Application.Interfaces.Repositories;
using Order.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using Order.Domain.Enums;

using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OrderEntity order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task<OrderEntity?> GetByIdAsync(Guid id)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<OrderEntity?> GetByIdAsync(Guid id, Guid customerId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        }

        public async Task<List<OrderEntity>> GetAllAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ToListAsync();
        }

        public Task<List<OrderEntity>> GetByStatusAsync(OrderStatus status)
        {
            return _context.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .Where(order => order.Status == status)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();
        }

        public Task<List<OrderEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return _context.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .Where(order => order.CreatedAt >= startDate && order.CreatedAt <= endDate)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<OrderEntity>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<OrderStatus?> QueueInventoryConfirmationAsync(Guid id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var order = await _context.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id);
            if (order is null)
                return null;

            if (order.Status == OrderStatus.Paid)
                return OrderStatus.Paid;

            if (order.Status == OrderStatus.Pending)
            {
                order.MarkPaymentConfirmationPending();
                var updated = await _context.Orders
                    .Where(item => item.Id == id && item.Status == OrderStatus.Pending)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(item => item.Status, OrderStatus.PaymentConfirmationPending)
                        .SetProperty(item => item.UpdatedAt, DateTime.UtcNow));
                if (updated != 1)
                    throw new InvalidOperationException("The order status changed while payment confirmation was being queued.");

                await _context.InventoryConfirmationOutbox.AddAsync(
                    InventoryConfirmationOutbox.Create(id, DateTime.UtcNow));
                await _context.SaveChangesAsync();
            }
            else if (order.Status != OrderStatus.PaymentConfirmationPending)
            {
                throw new InvalidOperationException($"Cannot confirm payment because order status is {order.Status}.");
            }
            else if (!await _context.InventoryConfirmationOutbox.AnyAsync(item => item.OrderId == id))
            {
                await _context.InventoryConfirmationOutbox.AddAsync(
                    InventoryConfirmationOutbox.Create(id, DateTime.UtcNow));
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return OrderStatus.PaymentConfirmationPending;
        }

        public Task<List<Guid>> GetDueInventoryConfirmationIdsAsync(DateTime nowUtc, int batchSize)
        {
            return _context.InventoryConfirmationOutbox
                .Where(item => item.NextAttemptAtUtc <= nowUtc)
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => item.OrderId)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task CompleteInventoryConfirmationAsync(Guid id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var exists = await _context.InventoryConfirmationOutbox
                .AnyAsync(item => item.OrderId == id);
            if (!exists)
                return;

            var status = await _context.Orders
                .Where(item => item.Id == id)
                .Select(item => (OrderStatus?)item.Status)
                .SingleOrDefaultAsync();
            if (status == OrderStatus.PaymentConfirmationPending)
            {
                await _context.Orders
                    .Where(item => item.Id == id && item.Status == OrderStatus.PaymentConfirmationPending)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(item => item.Status, OrderStatus.Paid)
                        .SetProperty(item => item.UpdatedAt, DateTime.UtcNow));
            }
            else if (status != OrderStatus.Paid)
            {
                throw new InvalidOperationException(
                    $"Cannot complete inventory confirmation because order status is {status?.ToString() ?? "missing"}.");
            }

            await _context.InventoryConfirmationOutbox
                .Where(item => item.OrderId == id)
                .ExecuteDeleteAsync();
            await transaction.CommitAsync();
        }

        public async Task RecordInventoryConfirmationFailureAsync(Guid id, DateTime nowUtc)
        {
            var outboxItem = await _context.InventoryConfirmationOutbox
                .SingleOrDefaultAsync(item => item.OrderId == id);
            if (outboxItem is null)
                return;

            outboxItem.RecordFailure(nowUtc);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CancelPendingOrderAsync(Guid id, DateTime cutoffUtc)
        {
            var updated = await _context.Orders
                .Where(order => order.Id == id
                    && order.Status == OrderStatus.Pending
                    && order.CreatedAt <= cutoffUtc)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(order => order.Status, OrderStatus.Cancelled)
                    .SetProperty(order => order.UpdatedAt, DateTime.UtcNow));

            return updated == 1;
        }

        public Task<List<Guid>> GetPendingOrderIdsAsync()
        {
            return _context.Orders
                .Where(order => order.Status == OrderStatus.Pending)
                .OrderBy(order => order.CreatedAt)
                .Select(order => order.Id)
                .Take(1000)
                .ToListAsync();
        }

        public async Task<bool> MarkPendingOrderPaidAsync(Guid id)
        {
            var updated = await _context.Orders
                .Where(order => order.Id == id && order.Status == OrderStatus.Pending)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(order => order.Status, OrderStatus.Paid)
                    .SetProperty(order => order.UpdatedAt, DateTime.UtcNow));

            return updated == 1;
        }

        public async Task UpdateAsync(OrderEntity order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var order = await GetByIdAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}
