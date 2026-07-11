namespace AsyncProcessingApp.Models;
public class MainTopicEvent
{
    public static readonly string DefaultAreaCode = "OTHER";

    public Guid Id { get; set; }

    public int Content { get; set; }

    public string AreaCode { get; set; } = DefaultAreaCode;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset SentAt { get; set; }
}
