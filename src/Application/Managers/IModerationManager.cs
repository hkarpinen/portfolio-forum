using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface IModerationManager
{
    Task<BanDto> BanAsync(BanUserCommand command, CancellationToken cancellationToken = default);
    Task<BanDto?> UnbanAsync(UnbanUserCommand command, CancellationToken cancellationToken = default);
    Task<ModerationLogEntryDto> LogAsync(LogModerationActionCommand command, CancellationToken cancellationToken = default);
}
