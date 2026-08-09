using Forum.Application.Repositories;
using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class ReportRepository : IReportRepository
{
    private readonly ForumDbContext _dbContext;

    public ReportRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Report?> GetByIdAsync(ReportId id, CancellationToken cancellationToken = default)
        => _dbContext.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Report>> ListOpenByTargetAsync(
        CommunityId communityId,
        ReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
        => await _dbContext.Reports
            .Where(r => r.CommunityId == communityId
                && r.TargetType == targetType
                && r.TargetId == targetId
                && r.Status == ReportStatus.Open)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Report report, CancellationToken cancellationToken = default)
    {
        await _dbContext.Reports.AddAsync(report, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
