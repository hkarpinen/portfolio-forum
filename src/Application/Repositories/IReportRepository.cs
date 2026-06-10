using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(ReportId id, CancellationToken cancellationToken = default);
    Task AddAsync(Report report, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
