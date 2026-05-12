using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;
using Forum.Application;

namespace Forum.Application.Managers;

internal sealed class ThreadWorkflowManager : IThreadWorkflowManager
{
    private readonly IThreadRepository _threadRepository;

    public ThreadWorkflowManager(IThreadRepository threadRepository)
    {
        _threadRepository = threadRepository;
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
            command.Content);

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

        thread.Edit(command.Title, command.Content, DateTime.UtcNow);
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
}
