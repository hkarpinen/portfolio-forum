using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(ReportId id, CancellationToken cancellationToken = default);

    /// <summary>A moderator acts on the content, so resolving must close every open
    /// report on it or the item comes straight back.</summary>
    Task<IReadOnlyList<Report>> ListOpenByTargetAsync(
        CommunityId communityId,
        ReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Report report, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
