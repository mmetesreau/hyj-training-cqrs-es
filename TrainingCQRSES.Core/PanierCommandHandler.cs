namespace TrainingCQRSES.Core;

public class PanierCommandHandler
{
    private readonly IEventStore _eventStore;
    private readonly IEventPublisher _eventPublisher;

    public PanierCommandHandler(IEventStore eventStore, IEventPublisher eventPublisher)
    {
        _eventStore = eventStore;
        _eventPublisher = eventPublisher;
    }

    public void Handle(AjouterArticleCmd cmd)
    {
        var histoire = _eventStore.Get(cmd.IdentifiantPanier);

        var decisions = PanierAggregate.Recoit(cmd, histoire);
        
        _eventPublisher.Publish(decisions);
    }

    public void Handle(EnleverArticleCmd cmd)
    {
        var histoire = _eventStore.Get(cmd.IdentifiantPanier);

        var decisions = PanierAggregate.Recoit(cmd, histoire);
        
        _eventPublisher.Publish(decisions);
    }
}

public interface IEventStore
{
    void Save(IEvent[] events);
    IEvent[] Get(Guid aggregateId);
}

public interface IEventPublisher
{
    void Publish(IEvent[] events);
    void Subscribe<T>(Action<T> handler) where T : IEvent;
}