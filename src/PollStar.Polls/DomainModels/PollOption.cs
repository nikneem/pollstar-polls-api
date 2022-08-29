using HexMaster.DomainDrivenDesign;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using PollStar.Polls.Abstractions.DomainModels;

namespace PollStar.Polls.DomainModels;

public class PollOption : DomainModel<Guid>, IPollOption
{
    public int DisplayOrder { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public void Delete()
    {
        SetState(TrackingState.Deleted);
    }


    public PollOption(Guid id, string name, string? description, int displayOrder) : base(id)
    {
        DisplayOrder = displayOrder;
        Name = name;
        Description = description;
    }

    public PollOption(string name, string? description, int displayOrder) : base(Guid.NewGuid(), TrackingState.New)
    {
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
    }

}