using System;
using System.Collections.Generic;
using System.Linq;
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

public interface IEvent { };

public record ArticleAjouteEvt(Guid IdentifiantPanier, Article Article) : IEvent;

public record ArticleEnleveEvt(Guid IdentifiantPanier, Article Article) : IEvent;

public record PanierValideEvt(Guid IdentifiantPanier) : IEvent;

public class PanierInvalideException : Exception { }

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