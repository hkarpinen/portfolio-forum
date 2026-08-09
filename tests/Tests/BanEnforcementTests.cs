using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Managers;
using Forum.Application.Queries;
using Forum.Application.Repositories;
using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Tests;

/// <summary>
/// These cover the POSTING side of a ban: the Moderation screen promises "A ban hides their
/// future posts. The thread stays." Reading stays open — a ban is per community, not a
/// site-wide block.
/// </summary>
public class BanEnforcementTests
{
    private static readonly CommunityId Community = new(Guid.NewGuid());

    [Fact]
    public async Task CreateThread_BannedAuthor_Throws()
    {
        var authorId = Guid.NewGuid();
        var manager = BuildThreadManager(new FakeBanRepository(banned: true));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.CreateAsync(new CreateThreadCommand(
                CommunityId: Community.Value,
                CommunitySlug: "kitchen",
                Title: "Anyone want a knife sharpened",
                Content: "Reasonable rates",
                Tags: null,
                AuthorId: authorId)));
    }

    [Fact]
    public async Task CreateThread_UnbannedAuthor_Succeeds()
    {
        var manager = BuildThreadManager(new FakeBanRepository(banned: false));

        var result = await manager.CreateAsync(new CreateThreadCommand(
            CommunityId: Community.Value,
            CommunitySlug: "kitchen",
            Title: "Cast iron crust without the smoke alarm",
            Content: "What am I missing?",
            Tags: null,
            AuthorId: Guid.NewGuid()));

        Assert.NotEqual(default, result.ThreadId);
    }

    [Fact]
    public async Task CreateComment_BannedAuthor_Throws()
    {
        var thread = SeedThread();
        var manager = BuildCommentManager(thread, new FakeBanRepository(banned: true));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.CreateAsync(new CreateCommentCommand(
                ThreadId: thread.Id.Value,
                Content: "link in bio",
                ParentCommentId: null,
                AuthorId: Guid.NewGuid())));
    }

    [Fact]
    public async Task CreateComment_UnbannedAuthor_Succeeds()
    {
        var thread = SeedThread();
        var manager = BuildCommentManager(thread, new FakeBanRepository(banned: false));

        var commentId = await manager.CreateAsync(new CreateCommentCommand(
            ThreadId: thread.Id.Value,
            Content: "Dry the surface properly.",
            ParentCommentId: null,
            AuthorId: Guid.NewGuid()));

        Assert.NotEqual(default, commentId);
    }

    // --- Lock, pin and delete took a thread id and nothing else, so any member
    // of any community could act on any thread. These pin the guard down. ---

    [Fact]
    public async Task LockThread_CallerIsNotAModeratorOfThatCommunity_Throws()
    {
        var thread = SeedThread();
        var memberships = new FakeMembershipRepository();
        // A moderator, but of somewhere else entirely.
        memberships.Add(new CommunityId(Guid.NewGuid()), new UserId(Guid.NewGuid()), CommunityRole.Moderator);

        var manager = new ThreadWorkflowManager(
            new FakeThreadRepository(thread), new FakeCommunityRepository(), new FakeThreadQuery(),
            new FakeBanRepository(banned: false), memberships, new FakeModerationLogRepository());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.LockAsync(new LockThreadCommand(thread.Id.Value, Guid.NewGuid())));
        Assert.False(thread.IsLocked);
    }

    [Fact]
    public async Task LockThread_ModeratorOfThatCommunity_Succeeds()
    {
        var thread = SeedThread();
        var callerId = Guid.NewGuid();
        var memberships = new FakeMembershipRepository();
        memberships.Add(Community, new UserId(callerId), CommunityRole.Moderator);

        var manager = new ThreadWorkflowManager(
            new FakeThreadRepository(thread), new FakeCommunityRepository(), new FakeThreadQuery(),
            new FakeBanRepository(banned: false), memberships, new FakeModerationLogRepository());

        await manager.LockAsync(new LockThreadCommand(thread.Id.Value, callerId));

        Assert.True(thread.IsLocked);
    }

    [Fact]
    public async Task DeleteThread_AuthorWithNoModeratorRole_Succeeds()
    {
        var authorId = Guid.NewGuid();
        var thread = ForumThread.Create(
            Community, "kitchen", new UserId(authorId), "Mine to delete", "…", null);

        var manager = new ThreadWorkflowManager(
            new FakeThreadRepository(thread), new FakeCommunityRepository(), new FakeThreadQuery(),
            new FakeBanRepository(banned: false), new FakeMembershipRepository(), new FakeModerationLogRepository());

        await manager.DeleteAsync(new DeleteThreadCommand(thread.Id.Value, authorId));

        Assert.NotNull(thread.DeletedAt);
    }

    [Fact]
    public async Task DeleteThread_StrangerWithNoRole_Throws()
    {
        var thread = SeedThread();

        var manager = new ThreadWorkflowManager(
            new FakeThreadRepository(thread), new FakeCommunityRepository(), new FakeThreadQuery(),
            new FakeBanRepository(banned: false), new FakeMembershipRepository(), new FakeModerationLogRepository());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.DeleteAsync(new DeleteThreadCommand(thread.Id.Value, Guid.NewGuid())));
        Assert.Null(thread.DeletedAt);
    }

    private static ForumThread SeedThread() => ForumThread.Create(
        Community, "kitchen", new UserId(Guid.NewGuid()), "How do you get a crust on steak", "…", null);

    private static ThreadWorkflowManager BuildThreadManager(FakeBanRepository bans)
        => new(new FakeThreadRepository(), new FakeCommunityRepository(), new FakeThreadQuery(), bans,
            new FakeMembershipRepository(), new FakeModerationLogRepository());

    private static CommentWorkflowManager BuildCommentManager(ForumThread thread, FakeBanRepository bans)
        => new(new FakeCommentRepository(), new FakeThreadRepository(thread), new FakeCommunityRepository(), bans);

    // --- Hand-rolled in-memory fakes (test project has no mocking library). ---

    private sealed class FakeBanRepository : IBanRepository
    {
        private readonly bool _banned;
        public FakeBanRepository(bool banned) => _banned = banned;
        public Task<CommunityBan?> GetByIdAsync(BanId id, CancellationToken cancellationToken = default)
            => Task.FromResult<CommunityBan?>(null);
        public Task<bool> IsBannedAsync(CommunityId communityId, UserId userId, CancellationToken cancellationToken = default)
            => Task.FromResult(_banned);
        public Task AddAsync(CommunityBan ban, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(CommunityBan ban, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeThreadRepository : IThreadRepository
    {
        private readonly Dictionary<ThreadId, ForumThread> _threads = new();
        public FakeThreadRepository(params ForumThread[] seed)
        {
            foreach (var t in seed) _threads[t.Id] = t;
        }
        public Task<ForumThread?> GetByIdAsync(ThreadId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_threads.GetValueOrDefault(id));
        public Task AddAsync(ForumThread thread, CancellationToken cancellationToken = default)
        {
            _threads[thread.Id] = thread;
            return Task.CompletedTask;
        }
        public Task UpdateAsync(ForumThread thread, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(ThreadId id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCommentRepository : ICommentRepository
    {
        public Task<Comment?> GetByIdAsync(CommentId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Comment?>(null);
        public Task AddAsync(Comment comment, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(CommentId id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCommunityRepository : ICommunityRepository
    {
        public Task<Community?> GetByIdAsync(CommunityId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Community?>(null);
        public Task<Community?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult<Community?>(null);
        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task AddAsync(Community community, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Community community, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(CommunityId id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMembershipRepository : IMembershipRepository
    {
        private readonly List<CommunityMembership> _memberships = new();
        public void Add(CommunityId communityId, UserId userId, CommunityRole role)
            => _memberships.Add(CommunityMembership.Create(communityId, userId, role));
        public Task<CommunityMembership?> GetByUserAndCommunityAsync(UserId userId, CommunityId communityId, CancellationToken cancellationToken = default)
            => Task.FromResult(_memberships.FirstOrDefault(m => m.UserId == userId && m.CommunityId == communityId));
        public Task<CommunityMembership?> GetByIdAsync(MembershipId id, CancellationToken cancellationToken = default)
            => Task.FromResult<CommunityMembership?>(null);
        public Task AddAsync(CommunityMembership membership, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CommunityMembership membership, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(MembershipId id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeModerationLogRepository : IModerationLogRepository
    {
        public Task<ModerationLog?> GetByIdAsync(LogId id, CancellationToken cancellationToken = default)
            => Task.FromResult<ModerationLog?>(null);
        public Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Only the draft-cap read is on the create path; the rest is unreachable here.</summary>
    private sealed class FakeThreadQuery : IThreadQuery
    {
        public Task<int> CountDraftsByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
        public Task<ThreadListDto> ListAsync(ListThreadsCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<ThreadListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<ThreadDto?> GetDetailAsync(ThreadDetailCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<FeedListDto> ListFeedAsync(FeedCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<SearchDto> SearchAsync(SearchQueryCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<ThreadSummaryDto>> ListDraftsByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<ThreadDto?> GetDraftByIdAsync(Guid authorId, Guid threadId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
