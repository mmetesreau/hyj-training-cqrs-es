using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using TrainingCQRSES.Core;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class PanierCommandHandlerTests
{
    [Fact]
    public void Quand_je_rajoute_un_article_alors_le_panier_est_incremente()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new SimpleEventPublisher(eventStore.Object);

        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);

        var articleCommandHandler = new PanierCommandHandler(eventStore.Object, eventPublisher);

        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite(1), paniersQueryHandler.GetQuantity(IdentiantPanierA));
    }

    [Fact]
    public void Quand_j_enleve_un_article_alors_le_panier_est_decremente()
    {
        var eventStore = new InMemoryEventStore();
        var eventPublisher = new SimpleEventPublisher(eventStore);

        var paniersQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);
        eventPublisher.Subscribe<ArticleEnleveEvt>(paniersQueryHandler.Quand);

        var articleCommandHandler = new PanierCommandHandler(eventStore, eventPublisher);
        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));

        articleCommandHandler.Handle(new EnleverArticleCmd(IdentiantPanierA, ArticleA));

        Assert.Equal(new PanierQuantite(2), paniersQueryHandler.GetQuantity(IdentiantPanierA));
    }
}

public class AggregateIdMismatchException : Exception { }

public class AggregateEvents
{
    public readonly IEvent[] Events;
    public readonly int Version;

    public AggregateEvents(IEvent[] events, int version)
    {
        if (events.DistinctBy(x => x.IdentifiantPanier).Count() > 1)
            throw new AggregateIdMismatchException();
        
        Events = events;
        Version = version;
    }
}

public class VersionMismatchException : Exception { }

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<IEvent>> _data;

    public InMemoryEventStore()
    {
        _data = new Dictionary<Guid, List<IEvent>>();
    }

    public void Save(IEvent[] events)
    {
        foreach (var evt in events)
        {
            if (!_data.ContainsKey(evt.IdentifiantPanier))
                _data[evt.IdentifiantPanier] = new List<IEvent>();

            _data[evt.IdentifiantPanier].Add(evt);
        }
    }

    public void Save(AggregateEvents[] aggregatesEvents)
    {
        foreach (var aggregateEvents in aggregatesEvents)
        {
            if (!aggregateEvents.Events.Any()) return;

            if (Get(aggregateEvents.Events.First().IdentifiantPanier).Length != aggregateEvents.Version)
                throw new VersionMismatchException();
            
            Save(aggregateEvents.Events);
        }
    }

    public IEvent[] Get(Guid aggregateId)
    {
        return _data.ContainsKey(aggregateId) ? _data[aggregateId].ToArray() : Array.Empty<IEvent>();
    }
}