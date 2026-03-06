namespace ClaimsService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message) where T : class;
}