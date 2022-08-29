using Azure;
using Azure.Data.Tables;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Options;
using PollStar.Core;
using PollStar.Core.Configuration;
using PollStar.Polls.Abstractions.DomainModels;
using PollStar.Polls.Abstractions.Repositories;
using PollStar.Polls.DomainModels;
using PollStar.Polls.Repositories.Entities;

namespace PollStar.Polls.Repositories;

public class PollStarPollsRepository : IPollStarPollsRepository
{
    private readonly TableClient _tableClient;
    private readonly ICacheClient _cacheClient;
    private const string TableName = "polls";
    private const string PartitionKey = "poll";

    public async Task<List<IPoll>> GetListAsync(Guid sessionId)
    {
        var redisCacheKey = $"polls:list:{sessionId}";
        await _cacheClient.InvalidateAsync(redisCacheKey);
        var pollsList = await _cacheClient.GetOrInitializeAsync(() => GetPollsBySessionIdAsync(sessionId), redisCacheKey);
        return pollsList.OrderBy(p => p.DisplayOrder).ToList();
    }

    public async Task<IPoll> GetAsync(Guid pollId)
    {
        var pollOptions = new List<IPollOption>();
        var redisKeyPollOptions = $"polls:options:{pollId}";
        var pollOptionEntities = await _cacheClient.GetOrInitializeAsync(() => GetPollOptionsByPollIdAsync(pollId), redisKeyPollOptions);
        pollOptions.AddRange(pollOptionEntities.Select(po =>
            new PollOption(Guid.Parse(po.RowKey), po.Name, po.Description, po.DisplayOrder)));

        var redisCacheKeyPoll = $"polls:details:{pollId}";
        var entity = await _cacheClient.GetOrInitializeAsync(() => GetPollDetailsByPollIdAsync(pollId), redisCacheKeyPoll);

        return new Poll(
            Guid.Parse(entity.RowKey),
            Guid.Parse(entity.SessionId),
            entity.Name,
            entity.Description,
            entity.DisplayOrder,
            pollOptions,
            entity.IsActive);
    }
    public async Task<bool> CreateAsync(IPoll domainModel)
    {
        if (domainModel.TrackingState == TrackingState.New)
        {
            var actions = new List<TableTransactionAction>();
            var pollEntity = ToTableEntity(domainModel);
            await _tableClient.AddEntityAsync(pollEntity);
            foreach (var option in domainModel.Options)
            {
                actions.Add(new TableTransactionAction(TableTransactionActionType.Add, ToTableEntity(domainModel, option)));
            }
            var response = await _tableClient.SubmitTransactionAsync(actions);

            await _cacheClient.InvalidateAsync($"polls:list:{domainModel.SessionId}");
            await _cacheClient.InvalidateAsync($"polls:options:{domainModel.Id}");
            await _cacheClient.InvalidateAsync($"polls:details:{domainModel.Id}");

            return response.Value.All(r => !r.IsError);
        }

        return false;
    }
    public async Task<bool> UpdateAsync(IPoll domainModel)
    {
        if (domainModel.TrackingState == TrackingState.Touched || domainModel.TrackingState == TrackingState.Modified)
        {
            var actions = new List<TableTransactionAction>();
            if (domainModel.TrackingState == TrackingState.Modified)
            {
                var pollEntity = ToTableEntity(domainModel);
                await _tableClient.UpdateEntityAsync(pollEntity, ETag.All, TableUpdateMode.Replace);
                await _cacheClient.InvalidateAsync($"polls:list:{domainModel.SessionId}");
                await _cacheClient.InvalidateAsync($"polls:details:{domainModel.Id}");
            }

            foreach (var option in domainModel.Options)
            {
                var optionEntity = ToTableEntity(domainModel, option);
                if (option.TrackingState == TrackingState.New)
                {
                    actions.Add(new TableTransactionAction(TableTransactionActionType.Add, optionEntity));
                }

                if (option.TrackingState == TrackingState.Modified)
                {
                    actions.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, optionEntity));
                }

                if (option.TrackingState == TrackingState.Deleted)
                {
                    actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, optionEntity));
                }
            }

            if (actions.Count > 0)
            {
                var response = await _tableClient.SubmitTransactionAsync(actions);
                await _cacheClient.InvalidateAsync($"polls:options:{domainModel.Id}");
                return response.Value.All(r => !r.IsError);
            }

            return true;
        }

        return false;
    }

    public async Task<bool> DeactivateAll(Guid sessionId)
    {
        var pollsQuery = _tableClient.QueryAsync<PollTableEntity>($"{nameof(PollTableEntity.PartitionKey)} eq '{PartitionKey}' and {nameof(PollTableEntity.SessionId)} eq '{sessionId}' and {nameof(PollTableEntity.IsActive)} eq true");
        var actions = new List<TableTransactionAction>();
        await foreach (var page in pollsQuery.AsPages())
        {
            foreach (var pollEntity in page.Values)
            {
                pollEntity.IsActive = false;
                actions.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace, pollEntity));
            }
        }

        if (actions.Count > 0)
        {
            var response = await _tableClient.SubmitTransactionAsync(actions);
            return response.Value.All(r => !r.IsError);
        }

        return true;
    }

    private async Task<List<IPoll>> GetPollsBySessionIdAsync(Guid sessionId)
    {
        var polls = new List<IPoll>();
        var pollsQuery = _tableClient.QueryAsync<PollTableEntity>($"{nameof(PollTableEntity.PartitionKey)} eq '{PartitionKey}' and {nameof(PollTableEntity.SessionId)} eq '{sessionId}'");
        await foreach (var page in pollsQuery.AsPages())
        {
            polls.AddRange(page.Values.Select(po =>
                new Poll(
                    Guid.Parse(po.RowKey),
                    sessionId, 
                    po.Name, 
                    po.Description, 
                    po.DisplayOrder,
                    new List<IPollOption>(),
                     po.IsActive
                    )));
        }
        return polls;
    }
    private async Task<List<PollOptionTableEntity>> GetPollOptionsByPollIdAsync(Guid pollId)
    {
        var pollOptions = new List<PollOptionTableEntity>();
        var pollOptionsQuery = _tableClient.QueryAsync<PollOptionTableEntity>($"{nameof(PollOptionTableEntity.PartitionKey)} eq '{pollId}'");
        await foreach (var page in pollOptionsQuery.AsPages())
        {
            pollOptions.AddRange(page.Values);
        }
        return pollOptions;
    }
    private async Task<PollTableEntity> GetPollDetailsByPollIdAsync(Guid pollId)
    {
        var entity = await _tableClient.GetEntityAsync<PollTableEntity>(PartitionKey, pollId.ToString());
        return entity.Value;
    }

    private static PollTableEntity ToTableEntity(IPoll domainModel)
    {
        return new PollTableEntity
        {
            PartitionKey = PartitionKey,
            RowKey = domainModel.Id.ToString(),
            SessionId = domainModel.SessionId.ToString(),
            Name = domainModel.Name,
            Description = domainModel.Description,
            DisplayOrder = domainModel.DisplayOrder,
            IsActive = domainModel.IsActive,
            Timestamp = DateTimeOffset.UtcNow,
            ETag = ETag.All
        };
    }
    private static PollOptionTableEntity ToTableEntity(IPoll poll, IPollOption domainModel)
    {
        return new PollOptionTableEntity
        {
            PartitionKey = poll.Id.ToString(),
            RowKey = domainModel.Id.ToString(),
            Name = domainModel.Name,
            Description = domainModel.Description,
            DisplayOrder = domainModel.DisplayOrder,
            Timestamp = DateTimeOffset.UtcNow,
            ETag = ETag.All
        };
    }

    public PollStarPollsRepository( ICacheClientFactory cacheClientFactory, IOptions<AzureConfiguration> options)
    {
        _cacheClient = cacheClientFactory.CreateClient(Constants.DefaultCacheClientName);
        var accountName = options.Value.StorageAccount;
        var accountKey = options.Value.StorageKey;
        var storageUri = new Uri($"https://{accountName}.table.core.windows.net");
        _tableClient = new TableClient(
            storageUri,
            TableName,
            new TableSharedKeyCredential(accountName, accountKey));
    }
}