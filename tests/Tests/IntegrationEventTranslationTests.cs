using System.Reflection;
using Infrastructure.Messaging;

namespace Tests;

/// <summary>
/// That every domain event leaving forum actually becomes its wire type.
///
/// Forum does not publish its domain events. `ThreadCreated` leaves as `ForumThreadCreatedEvent`,
/// and notifications binds the latter. MassTransit routes on namespace + type name, so publishing
/// the untranslated event puts it on an exchange nobody subscribes to: no error, no dead letter,
/// no failing test — every downstream consumer just goes quiet.
///
/// That is exactly what happened when the hand-rolled outbox (which held the mapping) was replaced
/// without carrying the mapping across.
/// </summary>
public class IntegrationEventTranslationTests
{
    [Fact]
    public void EveryPublishedDomainEventTranslatesToItsWireType()
    {
        foreach (var domainType in IntegrationEvents.Published)
        {
            var instance = Blank(domainType);
            var translated = IntegrationEvents.TryTranslate(instance);

            Assert.True(translated is not null, $"{domainType.Name} has no wire type.");
            Assert.StartsWith("Forum", translated!.Value.Type.Name);
        }
    }

    /// <summary>An event this service keeps to itself must not be silently published raw.</summary>
    [Fact]
    public void AnUnmappedEventIsNotPublished()
        => Assert.Null(IntegrationEvents.TryTranslate(new { Nothing = true }));

    private static object Blank(Type t)
    {
        var ctor = t.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => Default(p.ParameterType)).ToArray();
        return ctor.Invoke(args);
    }

    private static object? Default(Type t)
    {
        if (t == typeof(string)) return "x";
        if (t == typeof(Guid)) return Guid.NewGuid();
        if (t == typeof(DateTime)) return DateTime.UtcNow;
        if (Nullable.GetUnderlyingType(t) is { } inner) return Default(inner);
        if (t.IsValueType) return Activator.CreateInstance(t);
        // A strongly-typed id: one Guid constructor.
        var ctor = t.GetConstructor([typeof(Guid)]);
        return ctor is not null ? ctor.Invoke([Guid.NewGuid()]) : null;
    }
}
