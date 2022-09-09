using Azure.Core;
using Azure.Messaging.WebPubSub;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PollStar.Core.Configuration;
using PollStar.Core.Events;
using PollStar.Polls.Abstractions.DataTransferObjects;
using PollStar.Polls.Abstractions.DomainModels;
using PollStar.Polls.Abstractions.Repositories;
using PollStar.Polls.Abstractions.Services;
using PollStar.Polls.DomainModels;
using PollStar.Polls.ErrorCodes;
using PollStar.Polls.Exceptions;

namespace PollStar.Polls.Services;

public class PollStarPollsService : IPollStarPollsService
{
    private readonly ILogger<PollStarPollsService> _logger;
    private readonly IPollStarPollsRepository _repository;
    private readonly IOptions<AzureConfiguration> _cloudConfiguration;

    public async Task<List<PollListItemDto>> GetPollsListAsync(Guid sessionId)
    {
        _logger.LogInformation("Fetching polls in session");
        var pollDomainModels = await _repository.GetListAsync(sessionId);
        _logger.LogInformation("Found {pollsCount} polls in session {sessionId}", pollDomainModels.Count, sessionId);

        var pollsList = new List<PollListItemDto>();
        pollsList.AddRange(pollDomainModels.Select(p => new PollListItemDto(p.Name)
        {
            Id = p.Id,
            Description = p.Description,
            DisplayOrder = p.DisplayOrder
        }));
        return pollsList;
    }

    public async Task<PollDto?> GetActivePollAsync(Guid sessionId)
    {
        _logger.LogInformation("Fetching active poll details");
        var activePoll = await _repository.GetActiveAsync(sessionId);
        if (activePoll != null)
        {
            return ToPollDto(activePoll);
        }

        return null;
    }

    public async Task<PollDto> GetPollDetailsAsync(Guid pollId)
    {
        _logger.LogInformation("Fetching poll details");
        var pollDomainModel = await _repository.GetAsync(pollId);
        return ToPollDto(pollDomainModel);
    }

    public async Task<PollDto> CreatePollAsync(CreatePollDto dto)
    {
        var poll = new Poll(dto.SessionId, dto.Name);
        poll.SetDescription(dto.Description);
        foreach (var option in dto.Options)
        {
            poll.AddOption(new PollOption(option.Name, option.Description, dto.Options.IndexOf(option)));
        }
        if (await _repository.CreateAsync(poll))
        {
            return ToPollDto(poll);
        }

        throw new PollStarPollException(PollStarPollErrorCode.PollPersistenceFailed, 
            "Failed to create poll in data persistence store");
    }

    public async Task<PollDto> UpdatePollAsync(Guid pollId, PollDto dto)
    {
        var poll = await _repository.GetAsync(pollId);
        poll.SetName(dto.Name);
        poll.SetDescription(dto.Description);
        foreach (var dtoOption in dto.Options)
        {
            var domainOption = poll.Options.FirstOrDefault(dm => dm.Id.Equals(dtoOption.Id));
            if (domainOption != null)
            {
                domainOption.SetName(dtoOption.Name);
                domainOption.SetDescription(dtoOption.Description);
            }
            if (domainOption == null)
            {
                poll.AddOption(new PollOption(dtoOption.Name, dtoOption.Description, poll.Options.Count));
            }
        }

        foreach (var existingOption in poll.Options.Where(x => x.TrackingState == TrackingState.Pristine))
        {
            existingOption.Delete();
        }
        if (await _repository.UpdateAsync(poll))
        {
            return ToPollDto(poll);
        }

        throw new PollStarPollException(PollStarPollErrorCode.PollPersistenceFailed,
            "Failed to create poll in data persistence store");
    }

    public Task<bool> DeletePollAsync(Guid pollId)
    {
        return _repository.DeleteAsync(pollId);
    }

    public async Task<PollDto?> ActivatePollAsync(Guid pollId)
    {
        _logger.LogInformation("Fetching poll details");
        var pollDomainModel = await _repository.GetAsync(pollId);
        if (await _repository.DeactivateAll(pollDomainModel.SessionId))
        {
            pollDomainModel.Activate();
            if (await _repository.UpdateAsync(pollDomainModel))
            {
                var dto =  ToPollDto(pollDomainModel);
                await BroadcastActivePoll(pollDomainModel.SessionId, dto);
                return dto;
            }
        }

        return null;
    }

    private async Task BroadcastActivePoll(Guid sessionId, PollDto dto)
    {
        var pubsubClient = new WebPubSubServiceClient(_cloudConfiguration.Value.WebPubSub, _cloudConfiguration.Value.PollStarHub);
        var payload = RealtimeEvent.FromDto("poll-activated", dto);
        await pubsubClient.SendToGroupAsync(sessionId.ToString(), payload, ContentType.ApplicationJson);
    }


    //public async Task<ISessionDto> UpdateSessionAsync(ISessionDto dto)
    //{
    //    _logger.LogInformation("Creating new poll star session domain model");
    //    var session = new Session(dto.Name);
    //    session.SetDescription(dto.Description);
    //    _logger.LogInformation("Domain model created, now persisting in persistence");
    //    if (await _repository.UpdateAsync(session))
    //    {
    //        return ToSessionDto(session);
    //    }
    //    throw new PollStarSessionException(PollStarSessionErrorCode.SessionPersistenceFailed,
    //        "Failed to update session in data persistence store");
    //}
    //public Task<bool> DeleteSessionAsync(Guid id)
    //{
    //    return _repository.DeleteAsync(id);
    //}

    private static PollDto ToPollDto(IPoll poll)
    {
        var options = new List<PollOptionDto>();
        options.AddRange(poll.Options.OrderBy(x=> x.DisplayOrder).Select(po => new PollOptionDto(po.Name)
        {
            Id = po.Id,
            Description = po.Description,
            DisplayOrder = po.DisplayOrder
        }));

        return new PollDto
        {
            Id = poll.Id,
            Name = poll.Name,
            DisplayOrder = poll.DisplayOrder,
            Description = poll.Description,
            Options = options
        };
    }

    public PollStarPollsService(
        ILogger<PollStarPollsService> logger,
        IPollStarPollsRepository repository,
        IOptions<AzureConfiguration> cloudConfiguration)
    {
        _logger = logger;
        _repository = repository;
        _cloudConfiguration = cloudConfiguration;
    }
}