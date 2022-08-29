using HexMaster.RedisCache;
using Microsoft.Extensions.DependencyInjection;
using PollStar.Polls.Abstractions.Repositories;
using PollStar.Polls.Abstractions.Services;
using PollStar.Polls.Repositories;
using PollStar.Polls.Services;

namespace PollStar.Polls;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPollStarPolls(this IServiceCollection services)
    {
        services.AddTransient<IPollStarPollsService, PollStarPollsService>();
        services.AddTransient<IPollStarPollsRepository, PollStarPollsRepository>();
        return services;
    }
}