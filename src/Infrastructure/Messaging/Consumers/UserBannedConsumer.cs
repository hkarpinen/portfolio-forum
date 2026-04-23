using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;
using Forum.Domain.ValueObjects;
using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class UserBannedConsumer : IConsumer<UserBannedEvent>
{
    private readonly ForumDbContext _dbContext;

    public UserBannedConsumer(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UserBannedEvent> context)
    {
        var message = context.Message;
        if (await IsProcessedAsync(message.Id, context.CancellationToken))
        {
            return;
        }

        var userId = new UserId(message.UserId);
        var existing = await _dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.Id == userId, context.CancellationToken);

        var projection = new UserProjection(
            userId,
            existing?.UserName ?? $"user_{message.UserId:N}",
            existing?.DisplayName,
            existing?.AvatarUrl,
            existing?.RegisteredAt ?? message.OccurredAt,
            isBanned: true);

        if (existing is null)
        {
            await _dbContext.UserProjections.AddAsync(projection, context.CancellationToken);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(projection);
        }

        _dbContext.ProcessedEvents.Add(new ProcessedEvent(message.Id, nameof(UserBannedEvent), DateTime.UtcNow));

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // duplicate delivery handled by unique key on processed_events.event_id
        }
    }

    private Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
        => _dbContext.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);
}