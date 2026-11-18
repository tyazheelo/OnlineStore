using Order.Domain.Entities;

namespace Order.Application.Interfaces.Repositories;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id);
    Task<List<SupportTicket>> GetByCustomerIdAsync(Guid customerId);
    Task<List<SupportTicket>> GetAllAsync();
    Task AddAsync(SupportTicket ticket);
    Task<bool> TryAssignAsync(Guid ticketId, Guid adminId);
    Task UpdateAsync(SupportTicket ticket);
}
