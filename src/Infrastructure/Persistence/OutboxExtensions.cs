using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forum.Domain.ValueObjects;

namespace Infrastructure.Persistence;

// Ids serialise FLAT (a bare Guid, not {"value":...}) — that is the shape consumers bind against.
// Every id in this service wraps a single Guid, so one factory covers them all and a NEW id type is
// flat from the day it is introduced, rather than needing another hand-written converter.
internal sealed class StronglyTypedIdConverter : JsonConverterFactory
{
    public override bool CanConvert(Type type) =>
        type.GetProperty("Value")?.PropertyType == typeof(Guid)
        && type.GetConstructor([typeof(Guid)]) is not null;

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions _) =>
        (JsonConverter)Activator.CreateInstance(typeof(Inner<>).MakeGenericType(type))!;

    private sealed class Inner<T> : JsonConverter<T>
    {
        private static readonly Func<Guid, T> Wrap = BuildWrap();
        private static readonly Func<T, Guid> Unwrap = BuildUnwrap();

        private static Func<Guid, T> BuildWrap()
        {
            var g = Expression.Parameter(typeof(Guid));
            return Expression.Lambda<Func<Guid, T>>(
                Expression.New(typeof(T).GetConstructor([typeof(Guid)])!, g), g).Compile();
        }

        private static Func<T, Guid> BuildUnwrap()
        {
            var id = Expression.Parameter(typeof(T));
            return Expression.Lambda<Func<T, Guid>>(Expression.Property(id, "Value"), id).Compile();
        }

        public override T Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => Wrap(r.GetGuid());
        public override void Write(Utf8JsonWriter w, T v, JsonSerializerOptions o) => w.WriteStringValue(Unwrap(v));
    }
}

internal static class OutboxExtensions
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new StronglyTypedIdConverter()
        }
    };

    /// <summary>Must be called BEFORE SaveChangesAsync so the event and the state
    /// change commit together.</summary>
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
