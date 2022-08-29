namespace PollStar.Polls.Abstractions.DataTransferObjects;

public class CreatePollDto
{
    public Guid SessionId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public List<PollOptionDto> Options { get; set; }

    public CreatePollDto()
    {
        Options = new List<PollOptionDto>();
    }
}