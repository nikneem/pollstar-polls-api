using HexMaster.DomainDrivenDesign.ChangeTracking;

namespace PollStar.Polls.Abstractions.DomainModels;

public interface IPoll
{
    Guid Id { get; }
    Guid SessionId { get; }
    string Name { get; }
    string? Description { get; }
    int DisplayOrder { get; }
    IReadOnlyList<IPollOption> Options { get; }
    bool IsActive { get; }
    TrackingState TrackingState { get; }
    void SetName(string value);
    void SetDescription(string? value);
    void AddOption(IPollOption option);
    void RemoveOption(Guid id);
    void Activate();
    void Deactivate();
}
