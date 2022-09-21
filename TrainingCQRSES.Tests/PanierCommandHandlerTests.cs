using TrainingCQRSES.Domain.Core;
using TrainingCQRSES.Infra;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class PanierCommandHandlerTests
{
    [Fact]
    public async void Quand_je_rajoute_un_article_alors_le_panier_est_incremente()
    {
        var eventStore = new InMemoryEventStore();
        
        var eventPublisher = new SimpleEventPublisher(eventStore);

        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);

        var articleCommandHandler = new PanierCommandHandler(eventStore, eventPublisher);

        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite  { NombreArticles = 1 }, paniersQueryHandler.GetQuantity(IdentiantPanierA));
    }

    [Fact]
    public async void Quand_j_enleve_un_article_alors_le_panier_est_decremente()
    {
        var eventStore = new InMemoryEventStore();
        
        var eventPublisher = new SimpleEventPublisher(eventStore);

        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);
        eventPublisher.Subscribe<ArticleEnleveEvt>(paniersQueryHandler.Quand);

        var articleCommandHandler = new PanierCommandHandler(eventStore, eventPublisher);
        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        await articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));

        await articleCommandHandler.Handle(new EnleverArticleCmd(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite { NombreArticles = 2 }, paniersQueryHandler.GetQuantity(IdentiantPanierA));
    }
}