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
    /// <summary>
    /// Caller-side guardrail on how many in-progress drafts a single author
    /// may hold at once. The check is small enough to live in the Manager
    /// rather than a dedicated policy Engine — promote it out if the rule
    /// grows (tier-based, community-scoped, etc.).
    /// </summary>
    private const int DraftCap = 50;

    private readonly IThreadRepository _threadRepository;
    private readonly ICommunityRepository _communityRepository;
    private readonly IThreadQuery _threadQuery;

    public ThreadWorkflowManager(
        IThreadRepository threadRepository,
        ICommunityRepository communityRepository,
        IThreadQuery threadQuery)
    {
        _threadRepository = threadRepository;
        _communityRepository = communityRepository;
        _threadQuery = threadQuery;
    }

    public async Task<ThreadMutationDto> CreateAsync(CreateThreadCommand command, CancellationToken cancellationToken = default)
    {
        if (SpamDetectionEngine.IsSpam(command.Content ?? command.Title, command.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

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

        thread.Delete(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> LockAsync(LockThreadCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Lock(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationDto?> PinAsync(PinThreadCommand command, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Pin(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        await _threadRepository.CommitAsync(cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    // ── Draft authoring lifecycle ────────────────────────────────────────────

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

        // The `ThreadCreated` event needs the community slug; the thread
        // aggregate only carries the CommunityId. Fetch the slug here so
        // the published-thread event consumers can build URLs without an
        // extra hop. Slug lookup is one read per publish — acceptable.
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
