namespace Models;

public class MyMessage
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public static class QueueConstants
{
    public const string DefaultQueueName = "my-queue";
}

