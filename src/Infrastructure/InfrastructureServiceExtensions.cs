using Forum.Application.Queries;
using Forum.Domain.Repositories;
using Infrastructure.Media;
using Infrastructure.Messaging.Consumers;
using Infrastructure.Persistence;
using Infrastructure.Queries;
using Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ForumDbContext>(options =>
            options.UseNpgsql(
                    configuration.GetConnectionString("Forum"),
                    npgsql => npgsql.MigrationsAssembly("Infrastructure"))
                .UseSnakeCaseNamingConvention());

        var rabbitConfig = configuration.GetSection("RabbitMq");
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<UserRegisteredConsumer>();
            x.AddConsumer<UserProfileUpdatedConsumer>();
            x.AddConsumer<UserBannedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = rabbitConfig["Host"] ?? "localhost";

                cfg.Host(host, h =>
                {
                    var username = rabbitConfig["Username"];
                    var password = rabbitConfig["Password"];

                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        h.Username(username);
                    }

                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        h.Password(password);
                    }
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<ICommunityRepository, CommunityRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IBanRepository, BanRepository>();
        services.AddScoped<IThreadRepository, ThreadRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<IModerationLogRepository, ModerationLogRepository>();
        services.AddScoped<IUserProjectionRepository, UserProjectionRepository>();
        services.AddScoped<IForumProfileRepository, ForumProfileRepository>();

        services.AddScoped<ICommunityQuery, CommunityQuery>();
        services.AddScoped<IThreadQuery, ThreadQuery>();
        services.AddScoped<ICommentQuery, CommentQuery>();
        services.AddScoped<ISearchQuery, SearchQuery>();
        services.AddScoped<IForumProfileQuery, ForumProfileQuery>();
        services.AddScoped<IMembershipQuery, MembershipQuery>();
        services.AddScoped<IModerationQuery, ModerationQuery>();

        services.AddSingleton<IMediaStore, LocalFileSystemMediaStore>();

        return services;
    }
}
