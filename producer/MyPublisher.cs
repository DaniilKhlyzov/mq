using MassTransit;
using Models;

namespace Producer;

public class MyPublisher
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public MyPublisher(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task Publish(string content)
    {
        var publishEndpoint = await _sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{QueueConstants.DefaultQueueName}"));
        await publishEndpoint.Send(new MyMessage 
        { 
            MessageId = Guid.NewGuid(), 
            Content = content 
        });
    }
}

