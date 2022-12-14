using PollStar.Polls.Abstractions.DomainModels;

namespace PollStar.Polls.Abstractions.Repositories;

public interface IPollStarPollsRepository
{
    Task<List<IPoll>> GetListAsync(Guid sessionId);
    Task<IPoll?> GetActiveAsync(Guid sessionId);
    Task<IPoll> GetAsync(Guid pollId);
    Task<bool> CreateAsync(IPoll domainModel);
    Task<bool> UpdateAsync(IPoll domainModel);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeactivateAll(Guid sessionId);
}