using MassTransit;
using Models;

namespace Consumer;

public class MyConsumer : IConsumer<MyMessage>
{
    public Task Consume(ConsumeContext<MyMessage> context)
    {
        Console.WriteLine($"[{context.Message.MessageId}] {context.Message.Content}");
        return Task.CompletedTask;
    }
}

