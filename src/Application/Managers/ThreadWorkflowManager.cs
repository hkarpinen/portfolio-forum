using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.ReadModels;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class ThreadWorkflowManager : IThreadWorkflowManager
{
    private readonly IThreadRepository _threadRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ISpamDetectionEngine _spamDetectionEngine;
    private readonly IHotRankingEngine _hotRankingEngine;
    private readonly IUserProjectionRepository _userProjectionRepository;

    public ThreadWorkflowManager(
        IThreadRepository threadRepository,
        IVoteRepository voteRepository,
        ISpamDetectionEngine spamDetectionEngine,
        IHotRankingEngine hotRankingEngine,
        IUserProjectionRepository userProjectionRepository)
    {
        _threadRepository = threadRepository;
        _voteRepository = voteRepository;
        _spamDetectionEngine = spamDetectionEngine;
        _hotRankingEngine = hotRankingEngine;
        _userProjectionRepository = userProjectionRepository;
    }

    public async Task<ThreadResponse> CreateAsync(CreateThreadRequest request, CancellationToken cancellationToken = default)
    {
        if (_spamDetectionEngine.IsSpam(request.Content ?? request.Title, request.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        var thread = ForumThread.Create(
            new CommunityId(request.CommunityId),
            new UserId(request.AuthorId),
            request.Title,
            request.Content);

        await _threadRepository.AddAsync(thread, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(new UserId(request.AuthorId), cancellationToken);
        return Map(thread, 0, 0, proj);
    }

    public async Task<ThreadResponse?> EditAsync(EditThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        if (_spamDetectionEngine.IsSpam(request.Content ?? request.Title, thread.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        thread.Edit(request.Title, request.Content, DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(thread.AuthorId, cancellationToken);
        return Map(thread, 0, 0, proj);
    }

    public async Task<ThreadResponse?> DeleteAsync(DeleteThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Delete(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(thread.AuthorId, cancellationToken);
        return Map(thread, 0, 0, proj);
    }

    public async Task<ThreadResponse?> LockAsync(LockThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Lock(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(thread.AuthorId, cancellationToken);
        return Map(thread, 0, 0, proj);
    }

    public async Task<ThreadResponse?> PinAsync(PinThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Pin(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(thread.AuthorId, cancellationToken);
        return Map(thread, 0, 0, proj);
    }

    private ThreadResponse Map(ForumThread thread, int score, int commentCount, UserProjection? proj)
    {
        var hotScore = _hotRankingEngine.CalculateHotScore(thread.CreatedAt, score, commentCount);
        return new ThreadResponse(
            thread.Id.Value,
            thread.CommunityId.Value,
            thread.AuthorId.Value,
            proj?.DisplayName ?? proj?.UserName,
            proj?.AvatarUrl,
            thread.Title,
            thread.Content,
            thread.CreatedAt,
            thread.EditedAt,
            thread.IsLocked,
            thread.IsPinned,
            thread.DeletedAt,
            hotScore,
            score);
    }
}
