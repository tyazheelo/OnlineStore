using Order.Domain.Enums;

namespace Order.Domain.Entities;

public sealed class SupportTicket
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid? AssignedAdminId { get; private set; }
    public SupportTicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public List<SupportMessage> Messages { get; private set; } = [];

    private SupportTicket()
    {
    }

    public static SupportTicket Create(
        Guid orderId,
        string orderNumber,
        Guid customerId,
        string customerName,
        string message)
    {
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            Status = SupportTicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ticket.AddMessage(customerId, customerName, isAdmin: false, message);
        return ticket;
    }

    public void AssignTo(Guid adminId)
    {
        if (adminId == Guid.Empty)
            throw new ArgumentException("Admin ID is required.", nameof(adminId));
        if (Status == SupportTicketStatus.Resolved)
            throw new InvalidOperationException("A resolved request cannot be assigned.");
        if (AssignedAdminId is not null && AssignedAdminId != adminId)
            throw new InvalidOperationException("This request is already assigned to another administrator.");

        AssignedAdminId = adminId;
        Status = SupportTicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMessage(Guid authorId, string authorName, bool isAdmin, string message)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentException("Author ID is required.", nameof(authorId));
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length < 2 || message.Length > 2000)
            throw new ArgumentException("Message must contain between 2 and 2000 characters.", nameof(message));
        if (!isAdmin && Status == SupportTicketStatus.Resolved)
            throw new InvalidOperationException("A resolved request cannot receive customer replies.");
        if (isAdmin && AssignedAdminId != authorId)
            throw new InvalidOperationException("Assign the request to yourself before replying.");

        Messages.Add(SupportMessage.Create(Id, authorId, authorName.Trim(), isAdmin, message.Trim()));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve(Guid adminId)
    {
        if (Status == SupportTicketStatus.Resolved)
            return;
        if (AssignedAdminId != adminId)
            throw new InvalidOperationException("Assign the request to yourself before resolving it.");

        Status = SupportTicketStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }
}
