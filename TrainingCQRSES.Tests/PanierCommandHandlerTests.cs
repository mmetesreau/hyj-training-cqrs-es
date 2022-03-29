using Moq;
using TrainingCQRSES;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class PanierCommandHandlerTests
{
    // docker run --name esdb-node -it -p 2113:2113 -p 1113:1113 eventstore/eventstore:latest --insecure --run-projections=All --enable-external-tcp --enable-atom-pub-over-http
    // docker start esdb-node
    private const string EventStoreConnectionString =
        "esdb+discover://localhost:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
    
    [Fact]
    public async void Quand_je_rajoute_un_article_alors_le_panier_est_incremente()
    {
        // var eventStore = new Mock<IEventStore>().Object;
        var eventStore = new EventStoreDb(EventStoreConnectionString);
        
        var eventPublisher = new SimpleEventPublisher(eventStore);

        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);

        var articleCommandHandler = new PanierCommandHandler(eventStore, eventPublisher);

        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite(1), paniersQueryHandler.GetQuantity(IdentiantPanierA));
    }

    [Fact]
    public async void Quand_j_enleve_un_article_alors_le_panier_est_decremente()
    {
        // var eventStore = new InMemoryEventStore().Object;
        var eventStore = new EventStoreDb(EventStoreConnectionString);
        
        var eventPublisher = new SimpleEventPublisher(eventStore);

        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);
        eventPublisher.Subscribe<ArticleEnleveEvt>(paniersQueryHandler.Quand);

        var articleCommandHandler = new PanierCommandHandler(eventStore, eventPublisher);
        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));

        await articleCommandHandler.Handle(new EnleverArticleCmd(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite(2), paniersQueryHandler.GetQuantity(IdentiantPanierA));
    }
}