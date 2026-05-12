namespace Forum.Application.Commands;

public sealed record UpdateForumProfileCommand(string? Bio, string? Signature, Guid UserId = default);
public sealed record GetForumProfileCommand(Guid UserId);
