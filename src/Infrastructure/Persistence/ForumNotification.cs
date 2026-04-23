using System.Text.Json;

namespace Infrastructure.Persistence;

public sealed class ForumNotification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public JsonDocument Payload { get; private set; } = JsonDocument.Parse("{}");
    public bool Read { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ForumNotification()
    {
    }

    public ForumNotification(Guid id, Guid userId, string type, JsonDocument payload, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Payload = payload;
        CreatedAt = createdAt;
        Read = false;
    }
}