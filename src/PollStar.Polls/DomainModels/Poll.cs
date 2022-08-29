using HexMaster.DomainDrivenDesign;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using PollStar.Polls.Abstractions.DomainModels;
using PollStar.Polls.ErrorCodes;
using PollStar.Polls.Exceptions;

namespace PollStar.Polls.DomainModels;

public class Poll : DomainModel<Guid>, IPoll
{
    private List<IPollOption> _options;
    public Guid SessionId { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public IReadOnlyList<IPollOption> Options => _options.AsReadOnly();
    public bool IsActive { get; private set; }

    public void SetName(string value)
    {
        if (IsNullOrEmpty(value) || value.Length > 30)
        {
            throw new PollStarPollException(PollStarPollErrorCode.PollNameInvalid,
                "The name of the session is invalid. It must contain at least one and max. 30 characters");
        }

        if (!Equals(Name, value))
        {
            Name = value;
            SetState(TrackingState.Modified);
        }
    }
    public void SetDescription(string? value)
    {
        if (value != null && value.Length > 150)
        {
            throw new PollStarPollException(PollStarPollErrorCode.PollDescriptionInvalid,
                "The name of the session is invalid. It may contain a max. of 150 characters");
        }
        if (!Equals(Description, value))
        {
            Description = value;
            SetState(TrackingState.Modified);
        }
    }

    public void Activate()
    {
            IsActive = true;
            SetState(TrackingState.Modified);
    }
    public void Deactivate()
    {
        if (IsActive == true)
        {
            IsActive = false;
            SetState(TrackingState.Modified);
        }
    }
    public void AddOption(IPollOption option)
    {
        if (_options.All(o => o.Id != option.Id))
        {
            _options.Add(option);
            SetState(TrackingState.Touched);
        }
    }
    public void RemoveOption(Guid id)
    {
        var option = _options.Find(opt => opt.Id == id);
        if (option != null)
        {
            option.Delete();
            SetState(TrackingState.Touched);
        }
    }

    public Poll(Guid id, Guid sessionId, string name, string? description, int displayOrder, List<IPollOption> options, bool active) : base(id)
    {
        SessionId = sessionId;
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        _options = options;
        IsActive = active;
    }
    public Poll(Guid sessionId, string name) : base(Guid.NewGuid(), TrackingState.New)
    {
        SessionId = sessionId;
        Name = name;
        DisplayOrder = 99;
        _options = new List<IPollOption>();
    }
}