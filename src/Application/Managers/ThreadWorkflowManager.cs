using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;
using Forum.Application;

namespace Forum.Application.Managers;

internal sealed class ThreadWorkflowManager : IThreadWorkflowManager
{
    /// <summary>Cap on in-progress drafts per author.</summary>
    private const int DraftCap = 50;

    private readonly IThreadRepository _threadRepository;
    private readonly ICommunityRepository _communityRepository;
    private readonly IThreadQuery _threadQuery;
    private readonly IBanRepository _banRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IModerationLogRepository _moderationLogRepository;

    public ThreadWorkflowManager(
        IThreadRepository threadRepository,
        ICommunityRepository communityRepository,
        IThreadQuery threadQuery,
        IBanRepository banRepository,
        IMembershipRepository membershipRepository,
        IModerationLogRepository moderationLogRepository)
    {
        _threadRepository = threadRepository;
        _communityRepository = communityRepository;
        _threadQuery = threadQuery;
        _banRepository = banRepository;
        _membershipRepository = membershipRepository;
        _moderationLogRepository = moderationLogRepository;
    }

    public async Task<ThreadMutationDto> CreateAsync(CreateThreadCommand command, CancellationToken cancellationToken = default)
    {
        if (SpamDetectionEngine.IsSpam(command.Content ?? command.Title, command.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        // The Moderation screen promises "a ban hides their future posts", so this is the guard
        // that makes it true — a CommunityBan row nothing checked would be a lie on screen.
        if (await _banRepository.IsBannedAsync(
                new CommunityId(command.CommunityId), new UserId(command.AuthorId), cancellationToken))
            throw new UnauthorizedAccessException("You are banned from this community.");

        var thread = ForumThread.Create(
            new CommunityId(command.CommunityId),
            command.CommunitySlug,
            new UserId(command.AuthorId),
            command.Title,
            command.Content,
            command.Tags);

        await _threadRepository.AddAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> EditAsync(EditThreadCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        if (SpamDetectionEngine.IsSpam(command.Content ?? command.Title, thread.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        thread.Edit(command.Title, command.Content, command.Tags, DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> DeleteAsync(DeleteThreadCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        var callerId = new UserId(command.CallerId);
        if (thread.AuthorId != callerId)
            await EnsureModeratorAsync(thread.CommunityId, callerId, cancellationToken);

        thread.Delete(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> LockAsync(LockThreadCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        var callerId = new UserId(command.CallerId);
        await EnsureModeratorAsync(thread.CommunityId, callerId, cancellationToken);

        thread.Lock(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);

        // Locking is logged: the community's log is public, and this is the action a
        // reader is most likely to ask about.
        await _moderationLogRepository.AddAsync(
            ModerationLog.Create(thread.CommunityId, ModerationAction.LockThread, callerId, null, thread.Title),
            cancellationToken);

        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> PinAsync(PinThreadCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        var callerId = new UserId(command.CallerId);
        await EnsureModeratorAsync(thread.CommunityId, callerId, cancellationToken);

        thread.Pin(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);

        await _moderationLogRepository.AddAsync(
            ModerationLog.Create(thread.CommunityId, ModerationAction.PinThread, callerId, null, thread.Title),
            cancellationToken);

        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    /// <summary>Must moderate the thread's OWN community — membership elsewhere proves nothing.</summary>
    private async Task EnsureModeratorAsync(CommunityId communityId, UserId callerId, CancellationToken cancellationToken)
    {
        var membership = await _membershipRepository.GetByUserAndCommunityAsync(callerId, communityId, cancellationToken);
        if (membership?.Role is not (CommunityRole.Owner or CommunityRole.Moderator))
            throw new UnauthorizedAccessException("Only a moderator or owner of this community can do that.");
    }

    public async Task<ThreadMutationDto> BeginDraftAsync(BeginDraftCommand command, CancellationToken cancellationToken = default)
    {
        var count = await _threadQuery.CountDraftsByAuthorAsync(command.AuthorId, cancellationToken);
        if (count >= DraftCap)
            throw new InvalidOperationException($"Draft cap reached ({DraftCap}).");

        var thread = ForumThread.BeginDraft(
            new CommunityId(command.CommunityId),
            new UserId(command.AuthorId),
            command.Title,
            command.Content,
            command.Tags);

        await _threadRepository.AddAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> ReviseDraftAsync(ReviseDraftCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        // Aggregate enforces both ownership AND status invariants —
        // Manager doesn't need to pre-check.
        thread.Revise(new UserId(command.AuthorId), command.Title, command.Content, command.Tags);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> PublishDraftAsync(PublishDraftCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        // The event carries the slug so consumers can build URLs; the aggregate holds
        // only the id, so it costs one read per publish.
        var community = await _communityRepository.GetByIdAsync(thread.CommunityId, cancellationToken);
        if (community is null)
            throw new InvalidOperationException($"Community {thread.CommunityId.Value} not found.");

        // Spam check at publish time, not at draft revision — gives the
        // author room to compose without false positives mid-edit.
        if (SpamDetectionEngine.IsSpam(thread.Content ?? thread.Title, command.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        thread.Publish(new UserId(command.AuthorId), community.Slug, DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<bool> AbandonDraftAsync(AbandonDraftCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return false;

        thread.Abandon(new UserId(command.AuthorId), DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return true;
    }
}
