using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces.Repositories;
using Order.Domain.Entities;
using Order.Domain.Enums;
using Order.Infrastructure.Data;

namespace Order.Infrastructure.Repositories;

public sealed class SupportTicketRepository : ISupportTicketRepository
{
    private readonly OrderDbContext _context;

    public SupportTicketRepository(OrderDbContext context)
    {
        _context = context;
    }

    public Task<SupportTicket?> GetByIdAsync(Guid id)
    {
        return _context.SupportTickets
            .Include(ticket => ticket.Messages.OrderBy(message => message.CreatedAt))
            .FirstOrDefaultAsync(ticket => ticket.Id == id);
    }

    public Task<List<SupportTicket>> GetByCustomerIdAsync(Guid customerId)
    {
        return _context.SupportTickets
            .Include(ticket => ticket.Messages.OrderBy(message => message.CreatedAt))
            .Where(ticket => ticket.CustomerId == customerId)
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .ToListAsync();
    }

    public Task<List<SupportTicket>> GetAllAsync()
    {
        return _context.SupportTickets
            .Include(ticket => ticket.Messages.OrderBy(message => message.CreatedAt))
            .OrderBy(ticket => ticket.Status)
            .ThenByDescending(ticket => ticket.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(SupportTicket ticket)
    {
        await _context.SupportTickets.AddAsync(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TryAssignAsync(Guid ticketId, Guid adminId)
    {
        var updated = await _context.SupportTickets
            .Where(ticket => ticket.Id == ticketId
                && ticket.AssignedAdminId == null
                && ticket.Status != SupportTicketStatus.Resolved)
            .ExecuteUpdateAsync(update => update
                .SetProperty(ticket => ticket.AssignedAdminId, adminId)
                .SetProperty(ticket => ticket.Status, SupportTicketStatus.InProgress)
                .SetProperty(ticket => ticket.UpdatedAt, DateTime.UtcNow));
        return updated == 1;
    }

    public async Task UpdateAsync(SupportTicket ticket)
    {
        foreach (var message in ticket.Messages)
        {
            var entry = _context.Entry(message);
            if (entry.State is EntityState.Detached or EntityState.Modified)
                entry.State = EntityState.Added;
        }

        await _context.SaveChangesAsync();
    }
}
