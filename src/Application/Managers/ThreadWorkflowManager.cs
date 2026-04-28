using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Forum.Application;

namespace Forum.Application.Managers;

internal sealed class ThreadWorkflowManager : IThreadWorkflowManager
{
    private readonly IThreadRepository _threadRepository;
    private readonly ISpamDetectionEngine _spamDetectionEngine;

    public ThreadWorkflowManager(
        IThreadRepository threadRepository,
        ISpamDetectionEngine spamDetectionEngine)
    {
        _threadRepository = threadRepository;
        _spamDetectionEngine = spamDetectionEngine;
    }

    public async Task<ThreadMutationResponse> CreateAsync(CreateThreadRequest request, CancellationToken cancellationToken = default)
    {
        if (_spamDetectionEngine.IsSpam(request.Content ?? request.Title, request.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        var thread = ForumThread.Create(
            new CommunityId(request.CommunityId),
            request.CommunitySlug,
            new UserId(request.AuthorId),
            request.Title,
            request.Content);

        await _threadRepository.AddAsync(thread, cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationResponse?> EditAsync(EditThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        if (_spamDetectionEngine.IsSpam(request.Content ?? request.Title, thread.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        thread.Edit(request.Title, request.Content, DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationResponse?> DeleteAsync(DeleteThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Delete(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationResponse?> LockAsync(LockThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Lock(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }

    public async Task<ThreadMutationResponse?> PinAsync(PinThreadRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        thread.Pin(DateTime.UtcNow);
        await _threadRepository.UpdateAsync(thread, cancellationToken);
        return ThreadResponseFactory.ToMutation(thread);
    }
}
