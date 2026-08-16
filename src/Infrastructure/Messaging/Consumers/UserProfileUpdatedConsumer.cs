using Domain.Events;
using Forum.Domain.Aggregates;
using Infrastructure.Persistence.Projections;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdated>
{
    private readonly ForumDbContext _dbContext;

    public UserProfileUpdatedConsumer(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UserProfileUpdated> context)
    {
        var message = context.Message;

        var userId = new UserId(message.UserId);
        var existing = await _dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.Id == userId, context.CancellationToken);

        var projection = new UserProjection(
            userId,
            existing?.UserName ?? $"user_{message.UserId:N}",
            message.DisplayName,
            message.AvatarUrl,
            existing?.RegisteredAt ?? message.OccurredAt,
            existing?.IsBanned ?? false);

        if (existing is null)
        {
            await _dbContext.UserProjections.AddAsync(projection, context.CancellationToken);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(projection);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}