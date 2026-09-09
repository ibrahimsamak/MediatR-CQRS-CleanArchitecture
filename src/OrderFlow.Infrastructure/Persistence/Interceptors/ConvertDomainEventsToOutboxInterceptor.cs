

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderFlow.Domain.Common;
using OrderFlow.Infrastructure.Persistence.Outbox;

namespace OrderFlow.Infrastructure.Persistence.Interceptors;

public sealed class ConvertDomainEventsToOutboxInterceptor: SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken=default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entitiesWithEvents = context.ChangeTracker
          .Entries<Entity<OrderFlow.Domain.Orders.OrderId>>()   // aggregates with events
          .Select(e => e.Entity)
          .Where(e => e.DomainEvents.Count > 0)
          .ToList();

        var messages = new List<OutboxMessage>();
        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                messages.Add(new OutboxMessage
                {
                    Type = domainEvent.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
                });
            }
            entity.ClearDomainEvents();
        }

        if (messages.Count > 0)
            context.Set<OutboxMessage>().AddRange(messages);


        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}