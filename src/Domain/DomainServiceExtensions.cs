using Microsoft.Extensions.DependencyInjection;
using Forum.Domain.Engines;

namespace Forum.Domain;

public static class DomainServiceExtensions
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddScoped<IHotRankingEngine, HotRankingEngine>();
        services.AddScoped<ISpamDetectionEngine, SpamDetectionEngine>();
        // Add other domain services as needed
        return services;
    }
}
