using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface IModerationManager
{
    Task<BanResponse> BanAsync(BanUserRequest request, CancellationToken cancellationToken = default);
    Task<BanResponse?> UnbanAsync(UnbanUserRequest request, CancellationToken cancellationToken = default);
    Task<ModerationLogEntryResponse> LogAsync(LogModerationActionRequest request, CancellationToken cancellationToken = default);
}
