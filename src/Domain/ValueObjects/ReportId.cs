namespace Forum.Domain.ValueObjects;

public readonly record struct ReportId(Guid Value)
{
    public static ReportId New() => new(Guid.NewGuid());
}
