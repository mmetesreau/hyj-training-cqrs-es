namespace TrainingCQRSES.Domain;

public interface IEventStore
{
    Task Save(IEvent[] events);
    Task<IEvent[]> Get(Guid aggregateId);
}