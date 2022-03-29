using System;
using System.Linq;
using TrainingCQRSES.Core;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class PanierTests
{
    IEvent[] AucuneHistoire = Array.Empty<IEvent>();

    [Fact]
    public void Quand_je_rajoute_un_article_alors_j_obtiens_un_evenement_ArticleAjoute()
    {
        var decisions = PanierAggregate.Recoit(new AjouterArticleCmd(IdentiantPanierA, ArticleA), AucuneHistoire);

        Assert.Equal(new[] {new ArticleAjouteEvt(IdentiantPanierA, ArticleA)}, decisions);
    }

    [Fact]
    public void Etant_donne_un_panier_avec_un_articleA_quand_je_valide_alors_j_obtiens_un_evenement_PanierValide()
    {
        var decisions = PanierAggregate.Recoit(new ValiderPanierCmd(IdentiantPanierA), new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        Assert.Equal(new[] {new PanierValideEvt(IdentiantPanierA)}, decisions);
    }

    [Fact]
    public void Etant_donne_un_panier_avec_un_articleA_quand_j_enleve_un_article_alors_j_obtiens_un_evenement_ArticleEnleve()
    {
        var decisions = PanierAggregate.Recoit(new EnleverArticleCmd(IdentiantPanierA, ArticleA), new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        Assert.Equal(new[] {new ArticleEnleveEvt(IdentiantPanierA, ArticleA)}, decisions);
    }

    [Fact]
    public void Etant_donne_un_panier_avec_un_articleA_quand_j_enleve_un_articleB_alors_je_n_obtiens_aucun_evenement()
    {
        var decisions = PanierAggregate.Recoit(new EnleverArticleCmd(IdentiantPanierA, ArticleB), new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        Assert.Equal(AucuneHistoire, decisions);
    }

    [Fact]
    public void Etant_donne_un_panier_vide_quand_je_valide_un_panier_alors_je_recois_une_erreur()
    {
        Assert.Throws<PanierInvalideException>(() =>
        {
            PanierAggregate.Recoit(new ValiderPanierCmd(IdentiantPanierA), AucuneHistoire);
        });
    }

    [Fact]
    public void Etant_donne_un_panier_avec_un_articleA_quand_j_enleve_un_articleA_deux_fois_alors_je_n_obtiens_aucun_evenement()
    {
        var histoire = new IEvent[] {new ArticleAjouteEvt(IdentiantPanierA, ArticleA)};

        var decisions = PanierAggregate.Recoit(new EnleverArticleCmd(IdentiantPanierA, ArticleA), histoire);

        histoire = histoire.Concat(decisions).ToArray();

        decisions = PanierAggregate.Recoit(new EnleverArticleCmd(IdentiantPanierA, ArticleA), histoire);

        Assert.Equal(Array.Empty<IEvent>(), decisions);
    }
}