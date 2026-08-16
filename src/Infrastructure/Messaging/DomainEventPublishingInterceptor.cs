using Forum.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Messaging;

/// <summary>
/// Publishes what the aggregates raised, in the transaction that saves them.
///
/// An interceptor rather than a <c>SaveChangesAsync</c> override: saving is what a DbContext does,
/// and a subclass that quietly does something else as well is a trap for whoever calls it.
///
/// MassTransit's bus outbox turns each <c>Publish</c> into a row in ITS outbox table on this same
/// context, so events commit with the aggregate and its delivery service sends them — which is why
/// there is no polling loop here to own.
///
/// What goes on the wire is the INTEGRATION event, not the domain event. Publishing the domain
/// event directly compiles and runs and silently reaches nobody.
/// </summary>
internal sealed class DomainEventPublishingInterceptor : SaveChangesInterceptor
{
    // Resolved when saving, not when constructed. Under UseBusOutbox the publish endpoint reaches
    // back for this same DbContext, so a constructor argument makes building the context require
    // building the endpoint require building the context — which hangs rather than throwing.
    private readonly IServiceProvider _services;

    public DomainEventPublishingInterceptor(IServiceProvider services) => _services = services;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return result;

        var aggregates = eventData.Context.ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0) return result;

        var publishEndpoint = _services.GetRequiredService<IPublishEndpoint>();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                // Forum publishes INTEGRATION events, not its domain events — see IntegrationEvents.
                // An event with no wire type is internal to this service; nothing subscribes to it.
                if (IntegrationEvents.TryTranslate(domainEvent) is not { } wire) continue;

                await publishEndpoint.Publish(wire.Message, wire.Type, cancellationToken);
            }

            aggregate.ClearDomainEvents();
        }

        return result;
    }
}
