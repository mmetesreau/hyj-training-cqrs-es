using System;
using System.Collections.Generic;
using TrainingCQRSES.Core.Panier;
using TrainingCQRSES.Core.Panier.Queries;
using Xunit;
using static EventSourcing.Tests.Data;
using Panier = TrainingCQRSES.Core.Panier.Queries.Panier;

namespace EventSourcing.Tests;

public class PaniersQueryHandlerTests
{
    [Fact]
    public void Quand_un_evenement_ArticleAjoute_est_leve_alors_le_panier_est_incremente()
    {
        var paniersQueryHandler = new PaniersQueryHandler(new PaniersInMemoryRepository());

        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));

        Assert.Equal(new Panier(1), paniersQueryHandler.Get(IdentiantPanierA));
        Assert.Equal(new Panier(0), paniersQueryHandler.Get(IdentiantPanierB));
    }

    [Fact]
    public void Quand_un_evenement_ArticleEnleve_est_leve_alors_le_panier_est_decremente()
    {
        var paniersQueryHandler = new PaniersQueryHandler(new PaniersInMemoryRepository());

        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));
        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));
        paniersQueryHandler.Quand(new ArticleAjouteEvt(IdentiantPanierA, ArticleA));
        paniersQueryHandler.Quand(new ArticleEnleveEvt(IdentiantPanierA, ArticleA));

        Assert.Equal(new Panier(2), paniersQueryHandler.Get(IdentiantPanierA));
        Assert.Equal(new Panier(0), paniersQueryHandler.Get(IdentiantPanierB));
    }
}

public class PaniersInMemoryRepository : IPaniersRepository
{
    private Dictionary<Guid, Panier> _data = new();

    public void Set(Guid id, Panier value) => _data[id] = value;

    public Panier Get(Guid id) => _data.ContainsKey(id) ? _data[id] : new Panier(0);
}