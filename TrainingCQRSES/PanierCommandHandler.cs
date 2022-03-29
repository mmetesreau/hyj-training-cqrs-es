namespace TrainingCQRSES;

public class PanierCommandHandler
{
    private readonly IEventStore _eventStore;
    private readonly IEventPublisher _eventPublisher;

    public PanierCommandHandler(IEventStore eventStore, IEventPublisher eventPublisher)
    {
        _eventStore = eventStore;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(AjouterArticleCmd cmd)
    {
        var histoire = await _eventStore.Get(cmd.IdentifiantPanier);

        var decisions = PanierAggregate.Recoit(cmd, histoire);
        
        await _eventPublisher.Publish(decisions);
    }

    public async Task Handle(EnleverArticleCmd cmd)
    {
        var histoire = await _eventStore.Get(cmd.IdentifiantPanier);

        var decisions = PanierAggregate.Recoit(cmd, histoire);
        
        await _eventPublisher.Publish(decisions);
    }
}

public interface IEventStore
{
    Task Save(IEvent[] events);
    Task<IEvent[]> Get(Guid aggregateId);
}

public interface IEventPublisher
{
    Task Publish(IEvent[] events);
    void Subscribe<T>(Action<T> handler) where T : IEvent;
}