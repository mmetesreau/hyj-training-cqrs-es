namespace TrainingCQRSES.Domain.Core;

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

        var decisions = Panier.Recoit(cmd, histoire);
        
        await _eventPublisher.Publish(decisions);
    }

    public async Task Handle(EnleverArticleCmd cmd)
    {
        var histoire = await _eventStore.Get(cmd.IdentifiantPanier);

        var decisions = Panier.Recoit(cmd, histoire);
        
        await _eventPublisher.Publish(decisions);
    }
}