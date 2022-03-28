using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace EventSourcing.Tests;

public record PanierReadModel(int NombreArticles);

public interface IPaniersReadModelRepository
{
    void Set(Guid id, PanierReadModel value);
    PanierReadModel Get(Guid id);
}

public class PaniersReadModelInMemoryRepository : IPaniersReadModelRepository
{
    private Dictionary<Guid, PanierReadModel> _data = new();

    public void Set(Guid id, PanierReadModel value) => _data[id] = value;

    public PanierReadModel Get(Guid id) => _data.ContainsKey(id) ? _data[id] : new PanierReadModel(0);
}

public class PaniersReadModel
{
    private readonly IPaniersReadModelRepository _repository;

    public PaniersReadModel(IPaniersReadModelRepository repository)
    {
        _repository = repository;
    }

    public PanierReadModel Get(Guid identifiantPanier) => _repository.Get(identifiantPanier);

    public void Quand(ArticleAjouteEvt evt)
    {
        var panierReadModel = _repository.Get(evt.IdentifiantPanier);
        _repository.Set(evt.IdentifiantPanier,
            panierReadModel with {NombreArticles = panierReadModel.NombreArticles + 1});
    }

    public void Quand(ArticleEnleveEvt evt)
    {
        var panierReadModel = _repository.Get(evt.IdentifiantPanier);
        _repository.Set(evt.IdentifiantPanier,
            panierReadModel with {NombreArticles = panierReadModel.NombreArticles - 1});
    }
}

public record Article(string IdentifiantArticle);

public interface IEvent
{
};

public record ArticleAjouteEvt(Guid IdentifiantPanier, Article Article) : IEvent;

public record ArticleEnleveEvt(Guid IdentifiantPanier, Article Article) : IEvent;

public record PanierValideEvt(Guid IdentifiantPanier) : IEvent;

public class PanierInvalideException : Exception
{
}

public class Panier
{
    public class PanierDecisionProjection
    {
        private List<Article> _articles = new();

        public IReadOnlyList<Article> Articles
        {
            get => _articles;
        }

        public void Apply(IEvent evt)
        {
            switch (evt)
            {
                case ArticleAjouteEvt articleAjouteEvt:
                    _articles.Add(articleAjouteEvt.Article);
                    break;
                case ArticleEnleveEvt articleEnleveEvt:
                    _articles.Remove(articleEnleveEvt.Article);
                    break;
            }
        }
    }

    public static IEvent[] Recoit(AjouterArticleCmd cmd, IEvent[] histoire)
    {
        return new[] {new ArticleAjouteEvt(cmd.IdentifiantPanier, cmd.Article)};
    }

    public static IEvent[] Recoit(EnleverArticleCmd cmd, IEvent[] histoire)
    {
        var projection = new PanierDecisionProjection();
        foreach (var evt in histoire) projection.Apply(evt);

        if (projection.Articles.All(x => x != cmd.Article)) return Array.Empty<IEvent>();

        return new[] {new ArticleEnleveEvt(cmd.IdentifiantPanier, cmd.Article)};
    }

    public static IEvent[] Recoit(ValiderPanierCmd cmd, IEvent[] histoire)
    {
        var projection = new PanierDecisionProjection();
        foreach (var evt in histoire) projection.Apply(evt);

        if (!projection.Articles.Any()) throw new PanierInvalideException();

        return new[] {new PanierValideEvt(cmd.IdentifiantPanier)};
    }
}

public record AjouterArticleCmd(Guid IdentifiantPanier, Article Article);

public record EnleverArticleCmd(Guid IdentifiantPanier, Article Article);

public record ValiderPanierCmd(Guid IdentifiantPanier);

public interface IEventStore
{
    void Save(IEvent[] events);
}

public class EventPublisher
{
    private readonly IEventStore _eventStore;

    private Dictionary<Type, List<Action<IEvent>>> handlers = new();

    public EventPublisher(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public void Publish(IEvent[] newEvents)
    {
        _eventStore.Save(newEvents);

        foreach (var evt in newEvents)
        {
            if (!handlers.ContainsKey(evt.GetType())) continue;
            
            handlers[evt.GetType()].ForEach(handler => handler.Invoke(evt));
        }
    }

    public void Subscribe<T>(Action<T> quand) where T : IEvent
    {
        var tHandlers = handlers.ContainsKey(typeof(T)) ? handlers[typeof(T)] : new List<Action<IEvent>>();

        tHandlers.Add(x => quand((T)x));

        handlers[typeof(T)] = tHandlers;
    }
}

public class PubSubTests
{
    Guid IdentiantPanierA = new("9245fe4a-d402-451c-b9ed-9c1a04247482");
    Article UnArticle = new("A");

    [Fact]
    public void QuandUnEvenementEstPublieAlorsIlEstPersiste()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new EventPublisher(eventStore.Object);

        var histoire = new[] {new ArticleAjouteEvt(IdentiantPanierA, UnArticle)};
        eventPublisher.Publish(histoire);

        eventStore.Verify(x => x.Save(It.Is<IEvent[]>(evts => evts.SequenceEqual(histoire))));
    }

    [Fact]
    public void QuandUnEvenementEstPublieAlorsLesReadModelsAbonnesSontAppelles()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new EventPublisher(eventStore.Object);

        var handlerCalled = false;
        Action<ArticleAjouteEvt> handler = evt => handlerCalled = true;
        
        eventPublisher.Subscribe(handler);

        var histoire = new[] {new ArticleAjouteEvt(IdentiantPanierA, UnArticle)};
        eventPublisher.Publish(histoire);

        Assert.Equal(handlerCalled, true);
    }
    
    // todo: Integration test
}

public class PanierReadModelTests
{
    Guid IdentiantPanierA = new("9245fe4a-d402-451c-b9ed-9c1a04247482");
    Guid IdentiantPanierB = new("9245fe4a-d402-451c-b9ed-9c1a04247483");
    Article UnArticle = new("A");

    [Fact]
    public void QuandUnEvenementArticleAjouteEstLeveAlorsLePanierReadModelAssocieEstMisAJour()
    {
        var panierRepository = new PaniersReadModelInMemoryRepository();
        var panierReadModel = new PaniersReadModel(panierRepository);

        panierReadModel.Quand(new ArticleAjouteEvt(IdentiantPanierA, UnArticle));

        Assert.Equal(panierReadModel.Get(IdentiantPanierA), new PanierReadModel(1));
        Assert.Equal(panierReadModel.Get(IdentiantPanierB), new PanierReadModel(0));
    }

    [Fact]
    public void QuandUnEvenementArticleEnleveEstLeveAlorsLePanierReadModelAssocieEstMisAJour()
    {
        var panierRepository = new PaniersReadModelInMemoryRepository();
        var panierReadModel = new PaniersReadModel(panierRepository);

        panierReadModel.Quand(new ArticleAjouteEvt(IdentiantPanierA, UnArticle));
        panierReadModel.Quand(new ArticleAjouteEvt(IdentiantPanierA, UnArticle));
        panierReadModel.Quand(new ArticleAjouteEvt(IdentiantPanierA, UnArticle));
        panierReadModel.Quand(new ArticleEnleveEvt(IdentiantPanierA, UnArticle));

        Assert.Equal(panierReadModel.Get(IdentiantPanierA), new PanierReadModel(2));
        Assert.Equal(panierReadModel.Get(IdentiantPanierB), new PanierReadModel(0));
    }
}

public class PanierTests
{
    Guid UnIdentifiantPanier = Guid.NewGuid();
    Article ArticleA = new("A");
    Article ArticleB = new("B");

    [Fact]
    public void QuandJeRajouteUnArticleJObtiensUnEvenementArticleAjoute()
    {
        var aucuneHistoire = Array.Empty<IEvent>();

        var evenements = Panier.Recoit(new AjouterArticleCmd(UnIdentifiantPanier, ArticleA), aucuneHistoire);

        Assert.Equal(evenements, new[] {new ArticleAjouteEvt(UnIdentifiantPanier, ArticleA)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJeValideJObtiensUnEvenementPanierValide()
    {
        var histoire = new[] {new ArticleAjouteEvt(UnIdentifiantPanier, ArticleA)};

        var evenements = Panier.Recoit(new ValiderPanierCmd(UnIdentifiantPanier), histoire);

        Assert.Equal(evenements, new[] {new PanierValideEvt(UnIdentifiantPanier)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleAAlorsJObtiensUnEvenementArticleEnleve()
    {
        var histoire = new[] {new ArticleAjouteEvt(UnIdentifiantPanier, ArticleA)};

        var evenements = Panier.Recoit(new EnleverArticleCmd(UnIdentifiantPanier, ArticleA), histoire);

        Assert.Equal(evenements, new[] {new ArticleEnleveEvt(UnIdentifiantPanier, ArticleA)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleBAlorsJObtiensAucunEvenement()
    {
        var histoire = new[] {new ArticleAjouteEvt(UnIdentifiantPanier, ArticleA)};

        var evenements = Panier.Recoit(new EnleverArticleCmd(UnIdentifiantPanier, ArticleB), histoire);

        Assert.Equal(evenements, Array.Empty<IEvent>());
    }

    [Fact]
    public void EtantDonneUnPanierVideQuandJeValideUnPanierAlorsJeRecoisUneErreur()
    {
        var aucuneHistoire = Array.Empty<IEvent>();

        Assert.Throws<PanierInvalideException>(() =>
        {
            Panier.Recoit(new ValiderPanierCmd(UnIdentifiantPanier), aucuneHistoire);
        });
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleADeuxFoisAlorsJObtiensAucunEvenement()
    {
        var histoire = new IEvent[] {new ArticleAjouteEvt(UnIdentifiantPanier, ArticleA)};

        var evenements = Panier.Recoit(new EnleverArticleCmd(UnIdentifiantPanier, ArticleA), histoire);

        histoire = histoire.Concat(evenements).ToArray();

        evenements = Panier.Recoit(new EnleverArticleCmd(UnIdentifiantPanier, ArticleA), histoire);

        Assert.Equal(evenements, Array.Empty<IEvent>());
    }
}