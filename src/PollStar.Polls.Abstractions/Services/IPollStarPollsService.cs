using PollStar.Polls.Abstractions.DataTransferObjects;

namespace PollStar.Polls.Abstractions.Services;

public interface IPollStarPollsService
{
    Task<List<PollListItemDto>> GetPollsListAsync(Guid sessionId);
    Task<PollDto> GetPollAsync(Guid pollId);
    Task<PollDto> CreatePollAsync(CreatePollDto dto);
    Task<PollDto?> ActivatePollAsync(Guid pollId);
}