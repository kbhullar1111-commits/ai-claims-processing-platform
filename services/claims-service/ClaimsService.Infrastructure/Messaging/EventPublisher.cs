using MassTransit;
using ClaimsService.Application.Interfaces;

public class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message) where T : class
    {
        await _publishEndpoint.Publish(message);
    }
}