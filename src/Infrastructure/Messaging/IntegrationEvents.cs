using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forum.Domain.Events;
using Infrastructure.Messaging.Events;

namespace Infrastructure.Messaging;

/// <summary>
/// What forum puts on the bus, which is NOT what its aggregates raise.
///
/// This service has an integration layer: <c>ThreadCreated</c> leaves as
/// <c>ForumThreadCreatedEvent</c>, and every consumer in notifications binds the latter. Unlike
/// finance — which publishes its domain events directly and has no layer at all — dropping the
/// translation here does not fail loudly. MassTransit routes on namespace + type name, so an
/// untranslated event lands on an exchange nobody subscribes to and every downstream consumer
/// simply stops hearing anything.
///
/// The translation is a JSON round trip, exactly as the outbox publisher did it: the domain event
/// is written with camelCase and flattened ids, then read back as the wire record whose fields are
/// plain Guids. Field names must line up; that is the contract.
/// </summary>
internal static class IntegrationEvents
{
    private static readonly Dictionary<Type, Type> WireTypes = new()
    {
        [typeof(ThreadCreated)]                 = typeof(ForumThreadCreatedEvent),
        [typeof(CommentCreated)]                = typeof(ForumCommentCreatedEvent),
        [typeof(MembershipInvited)]             = typeof(ForumMembershipInvitedEvent),
        [typeof(MembershipJoined)]              = typeof(ForumMembershipJoinedEvent),
        [typeof(ModeratorAppointed)]            = typeof(ForumModeratorAppointedEvent),
        [typeof(ModeratorRemoved)]              = typeof(ForumModeratorRemovedEvent),
        [typeof(UserBanned)]                    = typeof(ForumUserBannedEvent),
        [typeof(UserUnbanned)]                  = typeof(ForumUserUnbannedEvent),
        [typeof(ThreadLocked)]                  = typeof(ForumThreadLockedEvent),
        [typeof(ThreadPinned)]                  = typeof(ForumThreadPinnedEvent),
        [typeof(CommunityOwnershipTransferred)] = typeof(ForumCommunityOwnershipTransferredEvent),
        [typeof(ModerationActionLogged)]        = typeof(ForumModerationActionLoggedEvent),
    };

    /// <summary>Every domain event that leaves this service, for the guard test to enumerate.</summary>
    internal static IReadOnlyCollection<Type> Published => WireTypes.Keys.ToList();

    /// <summary>Written with flattened ids, which is the shape the wire records expect.</summary>
    private static readonly JsonSerializerOptions DomainOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Enums as NAMES. The wire records declare them as strings, so a numeric enum cannot bind:
        // ModerationActionLogged failed exactly this way under the old publisher and dead-lettered
        // every moderation action, silently, because nothing read the dead-letter column.
        Converters = { new StronglyTypedIdConverter(), new JsonStringEnumConverter() },
    };

    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// The integration event for a domain event, or null when it is internal to this service and
    /// nothing outside subscribes to it.
    /// </summary>
    public static (object Message, Type Type)? TryTranslate(object domainEvent)
    {
        if (!WireTypes.TryGetValue(domainEvent.GetType(), out var wireType)) return null;

        var json = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), DomainOptions);
        var message = JsonSerializer.Deserialize(json, wireType, WireOptions)
            ?? throw new InvalidOperationException(
                $"{domainEvent.GetType().Name} did not translate into {wireType.Name} — the field names have drifted.");

        return (message, wireType);
    }
}

/// <summary>
/// Writes any single-Guid id as a flat guid rather than a <c>{"value":"…"}</c> envelope, so it
/// binds to the plain Guid field on the wire record.
/// </summary>
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
