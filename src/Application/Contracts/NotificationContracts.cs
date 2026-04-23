namespace Forum.Application.Contracts;

public sealed record NotificationStreamEventDto(
    Guid EventId,
    Guid RecipientUserId,
    string EventType,
    string Title,
    string? Message,
    string? DeepLink,
    DateTime OccurredAt,
    bool IsRead);

public sealed record NotificationStreamDto(
    IReadOnlyCollection<NotificationStreamEventDto> Items,
    string? ContinuationToken);
