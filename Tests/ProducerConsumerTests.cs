using MassTransit;
using MassTransit.Testing;
using Models;

namespace Tests;

public class ProducerConsumerTests
{

    [Fact]
    public async Task Producer_Should_Send_Message_To_Queue()
    {
        // Arrange
        var harness = new InMemoryTestHarness();
        harness.Consumer<MyConsumer>();

        await harness.Start();

        try
        {
            var sendEndpoint = await harness.GetSendEndpoint(new Uri($"queue:{QueueConstants.DefaultQueueName}"));
            var message = new MyMessage 
            { 
                MessageId = Guid.NewGuid(), 
                Content = "Test message" 
            };

            // Act
            await sendEndpoint.Send(message);

            // Assert
            Assert.True(await harness.Consumed.Any<MyMessage>());
            var consumed = harness.Consumed.Select<MyMessage>().First();
            Assert.Equal(message.MessageId, consumed.Context.Message.MessageId);
            Assert.Equal(message.Content, consumed.Context.Message.Content);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consumer_Should_Receive_Message_From_Queue()
    {
        // Arrange
        var harness = new InMemoryTestHarness();
        harness.Consumer<MyConsumer>();

        await harness.Start();

        try
        {
            var message = new MyMessage 
            { 
                MessageId = Guid.NewGuid(), 
                Content = "Test consumer message" 
            };

            // Act
            await harness.InputQueueSendEndpoint.Send(message);

            // Assert
            Assert.True(await harness.Consumed.Any<MyMessage>());
            var consumed = harness.Consumed.Select<MyMessage>().First();
            Assert.Equal(message.MessageId, consumed.Context.Message.MessageId);
            Assert.Equal(message.Content, consumed.Context.Message.Content);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Publisher_Should_Send_Message()
    {
        // Arrange
        var harness = new InMemoryTestHarness();
        harness.Consumer<MyConsumer>();

        await harness.Start();

        try
        {
            var sendEndpointProvider = harness.Bus;
            var publisher = new MyPublisher(sendEndpointProvider, QueueConstants.DefaultQueueName);
            var testContent = "Publisher test message";

            // Act
            await publisher.Publish(testContent);

            // Assert
            Assert.True(await harness.Consumed.Any<MyMessage>());
            var consumed = harness.Consumed.Select<MyMessage>().First();
            Assert.Equal(testContent, consumed.Context.Message.Content);
        }
        finally
        {
            await harness.Stop();
        }
    }
}

