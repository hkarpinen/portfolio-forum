using Forum.Application.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommunityWorkflowManager, CommunityWorkflowManager>();
        services.AddScoped<IMembershipManager, MembershipManager>();
        services.AddScoped<IThreadWorkflowManager, ThreadWorkflowManager>();
        services.AddScoped<ICommentWorkflowManager, CommentWorkflowManager>();
        services.AddScoped<IVoteManager, VoteManager>();
        services.AddScoped<IModerationManager, ModerationManager>();
        services.AddScoped<IForumProfileManager, ForumProfileManager>();

        return services;
    }
}
