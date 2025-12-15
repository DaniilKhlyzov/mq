using MassTransit;
using Models;

namespace Tests;

public class MyPublisher
{
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly string _queueName;

    public MyPublisher(ISendEndpointProvider sendEndpointProvider, string queueName = QueueConstants.DefaultQueueName)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _queueName = queueName;
    }

    public async Task Publish(string content)
    {
        var publishEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{_queueName}"));
        await publishEndpoint.Send(new MyMessage 
        { 
            MessageId = Guid.NewGuid(), 
            Content = content 
        });
    }
}

