
namespace PollStar.Polls.Abstractions.DataTransferObjects;

public class PollListItemDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public PollListItemDto(string name)
    {
        Name = name;
    }
}