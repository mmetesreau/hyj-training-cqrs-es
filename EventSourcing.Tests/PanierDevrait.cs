using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EventSourcing.Tests;

public record Article(string IdentifiantArticle);

public class Panier
{
    private List<Article> _articles;

    public Panier(IEvent[] histoire)
    {
        _articles = new List<Article>();

        foreach (var evt in histoire)
        {
            switch (evt)
            {
                case ArticleAjouteEvt articleAjouteEvt:
                    _articles.Add(articleAjouteEvt.Article);
                    break;
            }
        }
    }

    public IEvent[] Recoit(AjouterArticleCmd cmd)
    {
        return new[] {new ArticleAjouteEvt(cmd.Article)};
    }

    public IEvent[] Recoit(EnleverArticleCmd cmd)
    {
        if (_articles.Any(x => x == cmd.Article))
            return new[] {new ArticleEnleveEvt(cmd.Article)};

        return Array.Empty<IEvent>();
    }

    public IEvent[] Recoit(ValiderPanierCmd cmd)
    {
        if (_articles.Count == 0) throw new PanierInvalideException();
        
        return new[] {new PanierValideEvt()};
    }
}

public interface ICommand
{
}

public record AjouterArticleCmd(Article Article) : ICommand;

public record EnleverArticleCmd(Article Article) : ICommand;

public record ValiderPanierCmd() : ICommand;

public interface IEvent
{
}

public record ArticleAjouteEvt(Article Article) : IEvent;

public record ArticleEnleveEvt(Article Article) : IEvent;

public record PanierValideEvt() : IEvent;

public class PanierInvalideException : Exception
{
}

public class PanierDevrait
{
    Article ArticleA = new("A");
    Article ArticleB = new("B");

    [Fact]
    public void QuandJeRajoutesUnArticleJObtiensUnEvenementArticleAjoute()
    {
        var histoire = Array.Empty<IEvent>();
        var panier = new Panier(histoire);

        var evenements
            = panier.Recoit(new AjouterArticleCmd(ArticleA));

        Assert.Equal(evenements, new[] {new ArticleAjouteEvt(ArticleA)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJeValidesJObtiensUnPanierValide()
    {
        var histoire = new[] {new ArticleAjouteEvt(ArticleA)};
        var panier = new Panier(histoire);

        var evenements
            = panier.Recoit(new ValiderPanierCmd());

        Assert.Equal(evenements, new[] {new PanierValideEvt()});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleAAlorsJObtiensArticleEnleve()
    {
        var histoire = new[] {new ArticleAjouteEvt(ArticleA)};
        var panier = new Panier(histoire);

        var evenements
            = panier.Recoit(new EnleverArticleCmd(ArticleA));

        Assert.Equal(evenements, new[] {new ArticleEnleveEvt(ArticleA)});
    }

    [Fact]
    public void EtantDonneUnPanierAvecUnArticleAQuandJEnleveUnArticleBAlorsJObtiensAucunEvenement()
    {
        var histoire = new[] {new ArticleAjouteEvt(ArticleA)};
        var panier = new Panier(histoire);

        var evenements
            = panier.Recoit(new EnleverArticleCmd(ArticleB));

        Assert.Equal(evenements, Array.Empty<IEvent>());
    }

    [Fact]
    public void EtantDonneUnPanierVideQuandJeValideAlorsJeRetourneUneErreur()
    {
        var histoire = Array.Empty<IEvent>();
        var panier = new Panier(histoire);

        Assert.Throws<PanierInvalideException>(() => { panier.Recoit(new ValiderPanierCmd()); });
    }
}