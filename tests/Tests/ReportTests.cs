using Forum.Application.Commands;
using Forum.Application.Managers;
using Forum.Application.Repositories;
using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class ReportTests
{
    private static Report CreateReport(
        string reason = "Spam content",
        string? details = null,
        ReportTargetType targetType = ReportTargetType.Thread)
    {
        return Report.Create(
            new CommunityId(Guid.NewGuid()),
            targetType,
            Guid.NewGuid(),
            new UserId(Guid.NewGuid()),
            reason,
            details);
    }

    [Fact]
    public void Create_SetsProperties_AndIsOpen()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var targetId = Guid.NewGuid();
        var reporterId = new UserId(Guid.NewGuid());
        const string reason = "This is spam";
        const string details = "Detailed explanation here";

        var report = Report.Create(communityId, ReportTargetType.Thread, targetId, reporterId, reason, details);

        Assert.Equal(communityId, report.CommunityId);
        Assert.Equal(ReportTargetType.Thread, report.TargetType);
        Assert.Equal(targetId, report.TargetId);
        Assert.Equal(reporterId, report.ReporterId);
        Assert.Equal(reason, report.Reason);
        Assert.Equal(details, report.Details);
        Assert.Equal(ReportStatus.Open, report.Status);
        Assert.Null(report.ResolvedAt);
        Assert.Null(report.ResolvedByUserId);
        Assert.NotEqual(default, report.Id.Value);
        Assert.NotEqual(default(DateTime), report.ReportedAt);
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Report.Create(
                new CommunityId(Guid.NewGuid()),
                ReportTargetType.Comment,
                Guid.NewGuid(),
                new UserId(Guid.NewGuid()),
                "   ",
                null));
    }

    [Fact]
    public void Approve_FromOpen_TransitionsAndRaisesEvent()
    {
        var report = CreateReport();
        var moderatorId = new UserId(Guid.NewGuid());

        report.Approve(moderatorId);

        Assert.Equal(ReportStatus.Approved, report.Status);
        Assert.NotNull(report.ResolvedAt);
        Assert.Equal(moderatorId, report.ResolvedByUserId);

        var evt = Assert.Single(report.DomainEvents);
        var resolved = Assert.IsType<ReportResolved>(evt);
        Assert.Equal(report.Id, resolved.ReportId);
        Assert.Equal(ReportStatus.Approved, resolved.Status);
        Assert.Equal(moderatorId, resolved.ResolvedByUserId);
    }

    [Fact]
    public void Remove_FromOpen_TransitionsAndRaisesEvent()
    {
        var report = CreateReport(targetType: ReportTargetType.Comment);
        var moderatorId = new UserId(Guid.NewGuid());

        report.RemoveContent(moderatorId);

        Assert.Equal(ReportStatus.Removed, report.Status);
        Assert.NotNull(report.ResolvedAt);
        Assert.Equal(moderatorId, report.ResolvedByUserId);

        var evt = Assert.Single(report.DomainEvents);
        var resolved = Assert.IsType<ReportResolved>(evt);
        Assert.Equal(ReportStatus.Removed, resolved.Status);
        Assert.Equal(report.CommunityId, resolved.CommunityId);
        Assert.Equal(report.TargetId, resolved.TargetId);
    }

    [Fact]
    public void Dismiss_FromOpen_TransitionsAndRaisesEvent()
    {
        var report = CreateReport();
        var moderatorId = new UserId(Guid.NewGuid());

        report.Dismiss(moderatorId);

        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.NotNull(report.ResolvedAt);

        var evt = Assert.Single(report.DomainEvents);
        var resolved = Assert.IsType<ReportResolved>(evt);
        Assert.Equal(ReportStatus.Dismissed, resolved.Status);
    }

    [Fact]
    public void Approve_TwiceThrows()
    {
        var report = CreateReport();
        var moderatorId = new UserId(Guid.NewGuid());

        report.Approve(moderatorId);

        Assert.Throws<InvalidOperationException>(() => report.Approve(moderatorId));
    }

    [Fact]
    public void EventPayload_HasCorrectFields()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var targetId = Guid.NewGuid();
        var reporterId = new UserId(Guid.NewGuid());
        var report = Report.Create(communityId, ReportTargetType.Thread, targetId, reporterId, "Test reason", null);

        var moderatorId = new UserId(Guid.NewGuid());
        report.Dismiss(moderatorId);

        var evt = Assert.Single(report.DomainEvents);
        var resolved = Assert.IsType<ReportResolved>(evt);

        Assert.Equal(report.Id, resolved.ReportId);
        Assert.Equal(communityId, resolved.CommunityId);
        Assert.Equal(ReportTargetType.Thread, resolved.TargetType);
        Assert.Equal(targetId, resolved.TargetId);
        Assert.Equal(moderatorId, resolved.ResolvedByUserId);
        Assert.Equal(ReportStatus.Dismissed, resolved.Status);
        Assert.NotEqual(default(DateTime), resolved.ResolvedAt);
    }

    // --- Authorization: resolving a report requires a moderator/owner role in the
    // report's OWN community (regression guard for the privilege-escalation fix). ---

    private static (ModerationManager manager, Report report, FakeMembershipRepository memberships)
        BuildManagerWithOpenReport()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var report = Report.Create(
            communityId, ReportTargetType.Thread, Guid.NewGuid(),
            new UserId(Guid.NewGuid()), "Spam", null);

        var memberships = new FakeMembershipRepository();
        var manager = new ModerationManager(
            new FakeBanRepository(),
            new FakeModerationLogRepository(),
            new FakeReportRepository(report),
            memberships,
            new FakeThreadRepository(),
            new FakeCommentRepository());

        return (manager, report, memberships);
    }

    [Fact]
    public async Task ApproveReport_NonModeratorOfCommunity_Throws()
    {
        var (manager, report, _) = BuildManagerWithOpenReport();
        // Caller has no membership in the report's community at all.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.ApproveReportAsync(new ApproveReportCommand(report.Id.Value, Guid.NewGuid())));
        Assert.Equal(ReportStatus.Open, report.Status);
    }

    [Fact]
    public async Task RemoveContent_PlainMemberOfCommunity_Throws()
    {
        var (manager, report, memberships) = BuildManagerWithOpenReport();
        var callerId = Guid.NewGuid();
        // Caller is a member of the SAME community, but only role Member.
        memberships.Add(report.CommunityId, new UserId(callerId), CommunityRole.Member);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.RemoveContentAsync(new RemoveContentCommand(report.Id.Value, callerId)));
        Assert.Equal(ReportStatus.Open, report.Status);
    }

    [Fact]
    public async Task DismissReport_ModeratorOfDifferentCommunity_Throws()
    {
        var (manager, report, memberships) = BuildManagerWithOpenReport();
        var callerId = Guid.NewGuid();
        // Caller is a moderator, but of an UNRELATED community.
        memberships.Add(new CommunityId(Guid.NewGuid()), new UserId(callerId), CommunityRole.Moderator);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            manager.DismissReportAsync(new DismissReportCommand(report.Id.Value, callerId)));
        Assert.Equal(ReportStatus.Open, report.Status);
    }

    [Fact]
    public async Task ApproveReport_ModeratorOfReportCommunity_Succeeds()
    {
        var (manager, report, memberships) = BuildManagerWithOpenReport();
        var callerId = Guid.NewGuid();
        memberships.Add(report.CommunityId, new UserId(callerId), CommunityRole.Moderator);

        await manager.ApproveReportAsync(new ApproveReportCommand(report.Id.Value, callerId));

        Assert.Equal(ReportStatus.Approved, report.Status);
        Assert.Equal(new UserId(callerId), report.ResolvedByUserId);
    }

    [Fact]
    public async Task RemoveContent_OwnerOfReportCommunity_Succeeds()
    {
        var (manager, report, memberships) = BuildManagerWithOpenReport();
        var callerId = Guid.NewGuid();
        memberships.Add(report.CommunityId, new UserId(callerId), CommunityRole.Owner);

        await manager.RemoveContentAsync(new RemoveContentCommand(report.Id.Value, callerId));

        Assert.Equal(ReportStatus.Removed, report.Status);
    }

    // The queue is one row per TARGET, so resolving acts on the whole group — acting on one of
    // three reports must not leave two Open and put the card straight back on the screen.

    [Fact]
    public async Task RemoveContent_ClosesEveryOpenReportOnTheSameTarget()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var targetId = Guid.NewGuid();
        var reports = Enumerable.Range(0, 3)
            .Select(_ => Report.Create(
                communityId, ReportTargetType.Comment, targetId,
                new UserId(Guid.NewGuid()), "Advertising", null))
            .ToArray();

        // A report on OTHER content in the same community must be left alone.
        var unrelated = Report.Create(
            communityId, ReportTargetType.Comment, Guid.NewGuid(),
            new UserId(Guid.NewGuid()), "Off topic", null);

        var callerId = Guid.NewGuid();
        var memberships = new FakeMembershipRepository();
        memberships.Add(communityId, new UserId(callerId), CommunityRole.Moderator);

        var manager = new ModerationManager(
            new FakeBanRepository(),
            new FakeModerationLogRepository(),
            new FakeReportRepository(reports.Append(unrelated).ToArray()),
            memberships,
            new FakeThreadRepository(),
            new FakeCommentRepository());

        await manager.RemoveContentAsync(new RemoveContentCommand(reports[0].Id.Value, callerId));

        Assert.All(reports, r => Assert.Equal(ReportStatus.Removed, r.Status));
        Assert.Equal(ReportStatus.Open, unrelated.Status);
    }

    [Fact]
    public async Task RemoveContent_SoftDeletesTheReportedThread()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var thread = ForumThread.Create(
            communityId, "kitchen", new UserId(Guid.NewGuid()),
            "Best mattress for a small room?", "…", null);

        var report = Report.Create(
            communityId, ReportTargetType.Thread, thread.Id.Value,
            new UserId(Guid.NewGuid()), "Off topic", null);

        var callerId = Guid.NewGuid();
        var memberships = new FakeMembershipRepository();
        memberships.Add(communityId, new UserId(callerId), CommunityRole.Moderator);

        var manager = new ModerationManager(
            new FakeBanRepository(),
            new FakeModerationLogRepository(),
            new FakeReportRepository(report),
            memberships,
            new FakeThreadRepository(thread),
            new FakeCommentRepository());

        await manager.RemoveContentAsync(new RemoveContentCommand(report.Id.Value, callerId));

        // The point of the button: the content leaves the page, not just the queue.
        Assert.NotNull(thread.DeletedAt);
        Assert.Equal(ReportStatus.Removed, report.Status);
    }

    [Fact]
    public async Task RemoveContent_SoftDeletesTheReportedComment()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var comment = Comment.Create(
            new ThreadId(Guid.NewGuid()), new UserId(Guid.NewGuid()),
            "Sharpening service, link in bio", null);

        var report = Report.Create(
            communityId, ReportTargetType.Comment, comment.Id.Value,
            new UserId(Guid.NewGuid()), "Advertising", null);

        var callerId = Guid.NewGuid();
        var memberships = new FakeMembershipRepository();
        memberships.Add(communityId, new UserId(callerId), CommunityRole.Moderator);

        var manager = new ModerationManager(
            new FakeBanRepository(),
            new FakeModerationLogRepository(),
            new FakeReportRepository(report),
            memberships,
            new FakeThreadRepository(),
            new FakeCommentRepository(comment));

        await manager.RemoveContentAsync(new RemoveContentCommand(report.Id.Value, callerId));

        Assert.NotNull(comment.DeletedAt);
        Assert.Equal(ReportStatus.Removed, report.Status);
    }

    [Fact]
    public async Task RemoveContent_LogsTheGroupsDominantReason_NotTheNewestReports()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var comment = Comment.Create(
            new ThreadId(Guid.NewGuid()), new UserId(Guid.NewGuid()), "link in bio", null);

        // Two people said advertising, one said off topic. `Report.Create`
        // stamps ReportedAt itself, so creation order is chronological and the
        // off-topic one is the NEWEST — the report the queue names. The card
        // the moderator clicked said "Advertising", so that is what the public
        // log has to say too.
        var reports = new[]
        {
            MakeReport(communityId, comment.Id.Value, "Advertising"),
            MakeReport(communityId, comment.Id.Value, "Advertising"),
            MakeReport(communityId, comment.Id.Value, "Off topic"),
        };

        var callerId = Guid.NewGuid();
        var memberships = new FakeMembershipRepository();
        memberships.Add(communityId, new UserId(callerId), CommunityRole.Moderator);
        var logs = new FakeModerationLogRepository();

        var manager = new ModerationManager(
            new FakeBanRepository(),
            logs,
            new FakeReportRepository(reports),
            memberships,
            new FakeThreadRepository(),
            new FakeCommentRepository(comment));

        await manager.RemoveContentAsync(new RemoveContentCommand(reports[2].Id.Value, callerId));

        var entry = Assert.Single(logs.Entries);
        Assert.Equal(ModerationAction.DeleteComment, entry.Action);
        Assert.Equal("Advertising", entry.TargetContent);
    }

    private static Report MakeReport(CommunityId communityId, Guid targetId, string reason)
        => Report.Create(
            communityId, ReportTargetType.Comment, targetId,
            new UserId(Guid.NewGuid()), reason, null);

    [Fact]
    public async Task DismissReport_ClosesEveryOpenReportOnTheSameTarget()
    {
        var communityId = new CommunityId(Guid.NewGuid());
        var targetId = Guid.NewGuid();
        var first = Report.Create(
            communityId, ReportTargetType.Thread, targetId,
            new UserId(Guid.NewGuid()), "Off topic", null);
        var second = Report.Create(
            communityId, ReportTargetType.Thread, targetId,
            new UserId(Guid.NewGuid()), "Off topic", null);

        var callerId = Guid.NewGuid();
        var memberships = new FakeMembershipRepository();
        memberships.Add(communityId, new UserId(callerId), CommunityRole.Owner);

        var manager = new ModerationManager(
            new FakeBanRepository(),
            new FakeModerationLogRepository(),
            new FakeReportRepository(first, second),
            memberships,
            new FakeThreadRepository(),
            new FakeCommentRepository());

        await manager.DismissReportAsync(new DismissReportCommand(first.Id.Value, callerId));

        Assert.Equal(ReportStatus.Dismissed, first.Status);
        Assert.Equal(ReportStatus.Dismissed, second.Status);
    }

    // --- Hand-rolled in-memory fakes (test project has no mocking library). ---

    private sealed class FakeReportRepository : IReportRepository
    {
        private readonly Dictionary<ReportId, Report> _reports = new();
        public FakeReportRepository(params Report[] seed)
        {
            foreach (var r in seed) _reports[r.Id] = r;
        }
        public Task<Report?> GetByIdAsync(ReportId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_reports.GetValueOrDefault(id));
        public Task<IReadOnlyList<Report>> ListOpenByTargetAsync(
            CommunityId communityId, ReportTargetType targetType, Guid targetId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Report>>(_reports.Values
                .Where(r => r.CommunityId == communityId
                    && r.TargetType == targetType
                    && r.TargetId == targetId
                    && r.Status == ReportStatus.Open)
                .ToList());
        public Task AddAsync(Report report, CancellationToken cancellationToken = default)
        {
            _reports[report.Id] = report;
            return Task.CompletedTask;
        }
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
        private readonly Dictionary<CommentId, Comment> _comments = new();
        public FakeCommentRepository(params Comment[] seed)
        {
            foreach (var c in seed) _comments[c.Id] = c;
        }
        public Task<Comment?> GetByIdAsync(CommentId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_comments.GetValueOrDefault(id));
        public Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
        {
            _comments[comment.Id] = comment;
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(CommentId id, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
            => Task.FromResult(_memberships.FirstOrDefault(m => m.Id == id));
        public Task AddAsync(CommunityMembership membership, CancellationToken cancellationToken = default)
        {
            _memberships.Add(membership);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(CommunityMembership membership, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(MembershipId id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeModerationLogRepository : IModerationLogRepository
    {
        public List<ModerationLog> Entries { get; } = new();
        public Task<ModerationLog?> GetByIdAsync(LogId id, CancellationToken cancellationToken = default)
            => Task.FromResult<ModerationLog?>(null);
        public Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default)
        {
            Entries.Add(log);
            return Task.CompletedTask;
        }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeBanRepository : IBanRepository
    {
        public Task<CommunityBan?> GetByIdAsync(BanId id, CancellationToken cancellationToken = default)
            => Task.FromResult<CommunityBan?>(null);
        public Task<bool> IsBannedAsync(CommunityId communityId, UserId userId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task AddAsync(CommunityBan ban, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(CommunityBan ban, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
