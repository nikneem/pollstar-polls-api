
namespace PollStar.Polls.Abstractions.DataTransferObjects;

public class PollOptionDto 
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public PollOptionDto(string name)
    {
        Name = name;
    }
}