using Domain.Events;
using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Projections;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Messaging.Consumers;

/// <summary>
/// A demo account, projected the same way a registered one is.
///
/// Identity raises DemoUserCreated instead of UserRegistered for these, and forum listened only for
/// the latter — so a demo user had no projection row, and every thread they posted rendered its
/// author as "someone". The default ForumProfile matters for the same reason: queries assume one
/// exists.
/// </summary>
internal sealed class DemoUserCreatedConsumer : IConsumer<DemoUserCreated>
{
    private readonly ForumDbContext _dbContext;
    private readonly IForumProfileRepository _forumProfiles;

    public DemoUserCreatedConsumer(ForumDbContext dbContext, IForumProfileRepository forumProfiles)
    {
        _dbContext = dbContext;
        _forumProfiles = forumProfiles;
    }

    public async Task Consume(ConsumeContext<DemoUserCreated> context)
    {
        var message = context.Message;
        var userId = new UserId(message.UserId);

        var existing = await _dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.Id == userId, context.CancellationToken);

        if (existing is null)
        {
            await _dbContext.UserProjections.AddAsync(
                new UserProjection(
                    userId,
                    BuildUserName(message.Email, message.DisplayName, message.UserId),
                    message.DisplayName,
                    avatarUrl: null,
                    message.OccurredAt,
                    isBanned: false),
                context.CancellationToken);

            if (await _forumProfiles.GetByUserIdAsync(userId, context.CancellationToken) is null)
                await _forumProfiles.AddAsync(
                    ForumProfile.Create(userId, bio: null, signature: null), context.CancellationToken);
        }
        else
        {
            existing.DisplayName = message.DisplayName;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private static string BuildUserName(string? email, string? displayName, Guid userId)
    {
        if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
        {
            var local = email.Split('@', 2)[0].Trim();
            if (!string.IsNullOrWhiteSpace(local)) return local.ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim().ToLowerInvariant().Replace(' ', '_');

        return $"user_{userId:N}"[..16];
    }
}
