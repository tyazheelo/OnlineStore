using Order.Application.Interfaces.Repositories;
using Order.Application.Interfaces.Services;
using Order.Domain.Entities;
using Shop.Contracts.DTOs.Order;

namespace Order.Application.Services;

public sealed class SupportService : ISupportService
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IOrderRepository _orderRepository;

    public SupportService(ISupportTicketRepository ticketRepository, IOrderRepository orderRepository)
    {
        _ticketRepository = ticketRepository;
        _orderRepository = orderRepository;
    }

    public async Task<SupportTicketDTO> CreateAsync(
        Guid customerId,
        string customerName,
        Guid orderId,
        string message)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, customerId)
            ?? throw new KeyNotFoundException("Order not found.");
        var ticket = SupportTicket.Create(order.Id, order.OrderNumber, customerId, customerName, message);
        await _ticketRepository.AddAsync(ticket);
        return Map(ticket);
    }

    public async Task<List<SupportTicketDTO>> GetByCustomerIdAsync(Guid customerId)
    {
        var tickets = await _ticketRepository.GetByCustomerIdAsync(customerId);
        return tickets.Select(Map).ToList();
    }

    public async Task<List<SupportTicketDTO>> GetAllAsync()
    {
        var tickets = await _ticketRepository.GetAllAsync();
        return tickets.Select(Map).ToList();
    }

    public async Task<SupportTicketDTO?> GetByIdAsync(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        return ticket is null ? null : Map(ticket);
    }

    public async Task<SupportTicketDTO?> GetByIdAsync(Guid ticketId, Guid customerId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        return ticket is null || ticket.CustomerId != customerId ? null : Map(ticket);
    }

    public async Task<SupportTicketDTO?> AddMessageAsync(
        Guid ticketId,
        Guid authorId,
        string authorName,
        bool isAdmin,
        string message)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null || (!isAdmin && ticket.CustomerId != authorId))
            return null;

        ticket.AddMessage(authorId, authorName, isAdmin, message);
        await _ticketRepository.UpdateAsync(ticket);
        return Map(ticket);
    }

    public async Task<SupportTicketDTO?> AssignAsync(Guid ticketId, Guid adminId)
    {
        if (!await _ticketRepository.TryAssignAsync(ticketId, adminId))
        {
            var existing = await _ticketRepository.GetByIdAsync(ticketId);
            if (existing is null)
                return null;
            existing.AssignTo(adminId);
            await _ticketRepository.UpdateAsync(existing);
            return Map(existing);
        }

        var assigned = await _ticketRepository.GetByIdAsync(ticketId);
        return assigned is null ? null : Map(assigned);
    }

    public async Task<SupportTicketDTO?> ResolveAsync(Guid ticketId, Guid adminId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return null;

        ticket.Resolve(adminId);
        await _ticketRepository.UpdateAsync(ticket);
        return Map(ticket);
    }

    private static SupportTicketDTO Map(SupportTicket ticket)
    {
        return new SupportTicketDTO
        {
            Id = ticket.Id,
            OrderId = ticket.OrderId,
            OrderNumber = ticket.OrderNumber,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.CustomerName,
            AssignedAdminId = ticket.AssignedAdminId,
            Status = ticket.Status.ToString(),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Messages = ticket.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(message => new SupportMessageDTO
                {
                    Id = message.Id,
                    AuthorId = message.AuthorId,
                    AuthorName = message.AuthorName,
                    IsAdmin = message.IsAdmin,
                    Message = message.Message,
                    CreatedAt = message.CreatedAt
                })
                .ToList()
        };
    }
}
