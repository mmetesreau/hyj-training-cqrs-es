namespace TrainingCQRSES.Domain;

public interface IEventPublisher
{
    Task Publish(IEvent[] events);
    void Subscribe<T>(Action<T> handler) where T : IEvent;
}