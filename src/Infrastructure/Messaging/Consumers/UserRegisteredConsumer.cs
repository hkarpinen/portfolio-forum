using Domain.Events;
using Forum.Domain.Aggregates;
using Infrastructure.Persistence.Projections;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly ForumDbContext _dbContext;
    private readonly IForumProfileRepository _forumProfileRepository;

    public UserRegisteredConsumer(ForumDbContext dbContext, IForumProfileRepository forumProfileRepository)
    {
        _dbContext = dbContext;
        _forumProfileRepository = forumProfileRepository;
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var message = context.Message;
        if (await IsProcessedAsync(message.Id, context.CancellationToken))
            return;

        var userId = new UserId(message.UserId);
        var existing = await _dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.Id == userId, context.CancellationToken);

        var userName = existing?.UserName ?? BuildUserName(message.Email, message.DisplayName, message.UserId);
        var projection = new UserProjection(
            userId,
            userName,
            message.DisplayName,
            existing?.AvatarUrl,
            existing?.RegisteredAt ?? message.OccurredAt,
            existing?.IsBanned ?? false);

        if (existing is null)
        {
            await _dbContext.UserProjections.AddAsync(projection, context.CancellationToken);

            // Auto-create a default ForumProfile so queries never hit a null profile path
            var existingProfile = await _forumProfileRepository.GetByUserIdAsync(userId, context.CancellationToken);
            if (existingProfile is null)
            {
                var profile = ForumProfile.Create(userId, bio: null, signature: null);
                await _forumProfileRepository.AddAsync(profile, context.CancellationToken);
            }
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(projection);
        }

        _dbContext.ProcessedEvents.Add(new ProcessedEvent(message.Id, nameof(UserRegistered), DateTime.UtcNow));
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

    private static string BuildUserName(string? email, string? displayName, Guid userId)
    {
        if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
        {
            var local = email.Split('@', 2)[0].Trim();
            if (!string.IsNullOrWhiteSpace(local))
                return local.ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim().ToLowerInvariant().Replace(' ', '_');

        return $"user_{userId:N}";
    }
}
