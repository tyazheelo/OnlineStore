using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        Task<OrderEntity?> GetByIdAsync(Guid id);
        Task<OrderEntity?> GetByIdAsync(Guid id, Guid customerId);
        Task<List<OrderEntity>> GetAllAsync();
        Task<List<OrderEntity>> GetByStatusAsync(Order.Domain.Enums.OrderStatus status);
        Task<List<OrderEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<OrderEntity>> GetByCustomerIdAsync(Guid customerId);
        Task<Order.Domain.Enums.OrderStatus?> QueueInventoryConfirmationAsync(Guid id);
        Task<List<Guid>> GetDueInventoryConfirmationIdsAsync(DateTime nowUtc, int batchSize);
        Task CompleteInventoryConfirmationAsync(Guid id);
        Task RecordInventoryConfirmationFailureAsync(Guid id, DateTime nowUtc);
        Task<bool> CancelPendingOrderAsync(Guid id, DateTime cutoffUtc);
        Task<List<Guid>> GetPendingOrderIdsAsync();
        Task<bool> MarkPendingOrderPaidAsync(Guid id);
        Task AddAsync(OrderEntity order);
        Task UpdateAsync(OrderEntity order);
        Task DeleteAsync(Guid id);
    }
}
