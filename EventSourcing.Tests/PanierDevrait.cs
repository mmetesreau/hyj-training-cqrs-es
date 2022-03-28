using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EventSourcing.Tests;

public record Article(string IdentifiantArticle);

public class Panier
{
    public class PanierDecisionProjection
    {
        private List<Article> _articles = new ();

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
        return new[] {new ArticleAjouteEvt(cmd.Article)};
    }

    public static IEvent[] Recoit(EnleverArticleCmd cmd, IEvent[] histoire)
    {
        var projection = new PanierDecisionProjection();
        foreach (var evt in histoire) projection.Apply(evt);
        
        if (projection.Articles.All(x => x != cmd.Article)) return Array.Empty<IEvent>();
        
        return new[] {new ArticleEnleveEvt(cmd.Article)};
    }

    public static IEvent[] Recoit(ValiderPanierCmd cmd, IEvent[] histoire)
    {
        var projection = new PanierDecisionProjection();
        foreach (var evt in histoire) projection.Apply(evt);
        
        if (!projection.Articles.Any()) throw new PanierInvalideException();

        return new[] {new PanierValideEvt()};
    }
}

public interface ICommand { }

public record AjouterArticleCmd(Article Article) : ICommand;

public record EnleverArticleCmd(Article Article) : ICommand;

public record ValiderPanierCmd() : ICommand;

public interface IEvent { };

public record ArticleAjouteEvt(Article Article) : IEvent;

public record ArticleEnleveEvt(Article Article) : IEvent;

public record PanierValideEvt() : IEvent;

public class PanierInvalideException : Exception { }

public class PanierDevrait
{
    Article ArticleA = new("A");
    Article ArticleB = new("B");

    [Fact]
    public void QuandJeRajouteUnArticleJObtiensUnEvenementArticleAjoute()
    {
        var aucuneHistoire = Array.Empty<IEvent>();

        var evenements = Panier.Recoit(new AjouterArticleCmd(ArticleA), aucuneHistoire);

        Assert.Equal(evenements, new[] {new ArticleAjouteEvt(ArticleA)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJeValideJObtiensUnEvenementPanierValide()
    {
        var histoire = new[] {new ArticleAjouteEvt(ArticleA)};

        var evenements = Panier.Recoit(new ValiderPanierCmd(), histoire);

        Assert.Equal(evenements, new[] {new PanierValideEvt()});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleAAlorsJObtiensUnEvenementArticleEnleve()
    {
        var histoire = new[] {new ArticleAjouteEvt(ArticleA)};

        var evenements = Panier.Recoit(new EnleverArticleCmd(ArticleA), histoire);

        Assert.Equal(evenements, new[] {new ArticleEnleveEvt(ArticleA)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleBAlorsJObtiensAucunEvenement()
    {
        var histoire = new[] {new ArticleAjouteEvt(ArticleA)};

        var evenements = Panier.Recoit(new EnleverArticleCmd(ArticleB), histoire);

        Assert.Equal(evenements, Array.Empty<IEvent>());
    }

    [Fact]
    public void EtantDonneUnPanierVideQuandJeValideUnPanierAlorsJeRecoisUneErreur()
    {
        var aucuneHistoire = Array.Empty<IEvent>();

        Assert.Throws<PanierInvalideException>(() =>
        {
            Panier.Recoit(new ValiderPanierCmd(), aucuneHistoire);
        });
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleADeuxFoisAlorsJObtiensAucunEvenement()
    {
        var histoire = new IEvent[] { new ArticleAjouteEvt(ArticleA)};

        var evts = Panier.Recoit(new EnleverArticleCmd(ArticleA), histoire);

        histoire = histoire.Concat(evts).ToArray();
        
        evts = Panier.Recoit(new EnleverArticleCmd(ArticleA), histoire);

        Assert.Equal(evts, Array.Empty<IEvent>());
    }
}