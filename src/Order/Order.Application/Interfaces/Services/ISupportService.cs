using Shop.Contracts.DTOs.Order;

namespace Order.Application.Interfaces.Services;

public interface ISupportService
{
    Task<SupportTicketDTO> CreateAsync(Guid customerId, string customerName, Guid orderId, string message);
    Task<List<SupportTicketDTO>> GetByCustomerIdAsync(Guid customerId);
    Task<List<SupportTicketDTO>> GetAllAsync();
    Task<SupportTicketDTO?> GetByIdAsync(Guid ticketId);
    Task<SupportTicketDTO?> GetByIdAsync(Guid ticketId, Guid customerId);
    Task<SupportTicketDTO?> AddMessageAsync(Guid ticketId, Guid authorId, string authorName, bool isAdmin, string message);
    Task<SupportTicketDTO?> AssignAsync(Guid ticketId, Guid adminId);
    Task<SupportTicketDTO?> ResolveAsync(Guid ticketId, Guid adminId);
}
