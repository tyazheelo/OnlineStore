namespace Order.Domain.Entities;

public sealed class SupportMessage
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public bool IsAdmin { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private SupportMessage()
    {
    }

    internal static SupportMessage Create(
        Guid ticketId,
        Guid authorId,
        string authorName,
        bool isAdmin,
        string message)
    {
        return new SupportMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            AuthorName = authorName,
            IsAdmin = isAdmin,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
    }
}
