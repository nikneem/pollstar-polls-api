
using HexMaster.DomainDrivenDesign.ChangeTracking;

namespace PollStar.Polls.Abstractions.DomainModels;

public interface IPollOption
{
    string Name { get; }
    string? Description { get; }
    int DisplayOrder { get; }
    Guid Id { get; }
    TrackingState TrackingState { get; }
    void Delete();
}