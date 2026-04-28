using System.Text.Json;
using System.Text.Json.Serialization;
using Forum.Domain.ValueObjects;

namespace Infrastructure.Persistence;

// Value-object JSON converters so forum domain events serialise to flat Guid
// values rather than {"value":"..."} objects. The flat shape is what the wire
// records in Infrastructure.Messaging.Events expect.
internal sealed class ThreadIdConverter : JsonConverter<ThreadId>
{
    public override ThreadId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, ThreadId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal sealed class CommentIdConverter : JsonConverter<CommentId>
{
    public override CommentId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, CommentId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal sealed class CommunityIdConverter : JsonConverter<CommunityId>
{
    public override CommunityId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, CommunityId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal sealed class ForumUserIdConverter : JsonConverter<UserId>
{
    public override UserId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, UserId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal sealed class MembershipIdConverter : JsonConverter<MembershipId>
{
    public override MembershipId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, MembershipId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal sealed class BanIdConverter : JsonConverter<BanId>
{
    public override BanId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, BanId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal sealed class LogIdConverter : JsonConverter<LogId>
{
    public override LogId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, LogId v, JsonSerializerOptions o) => w.WriteStringValue(v.Value);
}

internal static class OutboxExtensions
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new ThreadIdConverter(),
            new CommentIdConverter(),
            new CommunityIdConverter(),
            new ForumUserIdConverter(),
            new MembershipIdConverter(),
            new BanIdConverter(),
            new LogIdConverter()
        }
    };

    /// <summary>
    /// Serialises a domain event and appends it to the outbox_messages table.
    /// Call this for every domain event before SaveChangesAsync so both writes
    /// occur in the same transaction.
    /// </summary>
    public static void AddToOutbox(this ForumDbContext context, object domainEvent)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
            CreatedAt = DateTime.UtcNow,
            Published = false
        };

        context.OutboxMessages.Add(message);
    }
}
