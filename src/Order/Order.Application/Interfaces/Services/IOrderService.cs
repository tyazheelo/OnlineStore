using Order.Application.Commands;
using Order.Application.Commands;
using Shop.Contracts.DTOs.Order;
using Shop.Contracts.Requests.Order;
using Order.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDTO> CheckoutAsync(CheckoutOrderCommand command);
        Task<List<OrderDTO>> GetAllOrdersAsync();
        Task<OrderDTO?> GetOrderByIdAsync(Guid id);
        Task<OrderDTO?> GetOrderByIdAsync(Guid id, Guid customerId);
        Task<List<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId);
        Task<OrderDTO?> UpdateOrderStatusAsync(Guid id, string status);
        Task<OrderDTO?> UpdateDeliveryAddressAsync(Guid id, Address address);
        Task DeleteOrderAsync(Guid id);
        Task<OrderDTO?> CancelOrderAsync(Guid id);
        Task<OrderDTO?> CancelOrderAsync(Guid id, Guid customerId);
        Task<List<OrderDTO>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<OrderDTO>> GetOrdersByStatusAsync(string status);
    }
}
