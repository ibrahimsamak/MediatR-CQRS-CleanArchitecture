

namespace OrderFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Type { get; init; }        // event CLR type name
    public required string Content { get; init; }      // JSON payload
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedOnUtc { get; set; }      // null = not yet dispatched
    public string? Error { get; set; }
}