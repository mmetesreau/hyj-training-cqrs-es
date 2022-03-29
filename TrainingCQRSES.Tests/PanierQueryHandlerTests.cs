using System;
using System.Collections.Generic;
using TrainingCQRSES.Core;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class PanierQueryHandlerTests
{
    [Fact]
    public void Quand_un_evenement_ArticleAjoute_est_leve_alors_le_panier_est_incremente()
    {
        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());

        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite(1), paniersQueryHandler.GetQuantity(IdentiantPanierA));
        Assert.Equal(new PanierQuantite(0), paniersQueryHandler.GetQuantity(IdentiantPanierB));
    }

    [Fact]
    public void Quand_un_evenement_ArticleEnleve_est_leve_alors_le_panier_est_decremente()
    {
        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());

        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));
        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));
        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));
        paniersQueryHandler.Quand(new ArticleEnleveEvt(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite(2), paniersQueryHandler.GetQuantity(IdentiantPanierA));
        Assert.Equal(new PanierQuantite(0), paniersQueryHandler.GetQuantity(IdentiantPanierB));
    }
}

public class PaniersInMemoryRepository : IPaniersRepository
{
    private Dictionary<Guid, PanierQuantite> _data = new();

    public void Set(Guid id, PanierQuantite value) => _data[id] = value;

    public PanierQuantite Get(Guid id) => _data.ContainsKey(id) ? _data[id] : new PanierQuantite(0);
}