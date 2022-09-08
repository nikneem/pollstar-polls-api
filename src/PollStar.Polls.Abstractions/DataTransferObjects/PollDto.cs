
namespace PollStar.Polls.Abstractions.DataTransferObjects;

public class PollDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
    public List<PollOptionDto> Options { get; set; }

    public PollDto()
    {
        Options = new List<PollOptionDto>();
    }
}