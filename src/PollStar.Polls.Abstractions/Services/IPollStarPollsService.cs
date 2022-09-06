using PollStar.Polls.Abstractions.DataTransferObjects;

namespace PollStar.Polls.Abstractions.Services;

public interface IPollStarPollsService
{
    Task<List<PollListItemDto>> GetPollsListAsync(Guid sessionId);
    Task<PollDto> GetPollDetailsAsync(Guid pollId);
    Task<PollDto> CreatePollAsync(CreatePollDto dto);
    Task<PollDto> UpdatePollAsync(Guid pollId, PollDto dto);
    Task<bool> DeletePollAsync(Guid pollId);
    Task<PollDto?> ActivatePollAsync(Guid pollId);
}