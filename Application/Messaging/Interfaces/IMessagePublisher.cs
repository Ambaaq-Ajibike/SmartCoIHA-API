namespace Application.Messaging.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, string queueName);
    }
}
