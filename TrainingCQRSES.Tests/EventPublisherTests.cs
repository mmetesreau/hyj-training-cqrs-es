using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using TrainingCQRSES.Core;
using TrainingCQRSES.Core.Panier;
using TrainingCQRSES.Core.Panier.Queries;
using Xunit;
using static EventSourcing.Tests.Data;

namespace EventSourcing.Tests;

public class EventPublisherTests
{
    [Fact]
    public void Quand_un_evenement_est_publie_alors_il_est_persiste()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new SimpleEventPublisher(eventStore.Object);

        eventPublisher.Publish(IdentiantPanierA, new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        eventStore.Verify(x => x.Save(It.Is<Guid>(id => id == IdentiantPanierA), It.Is<IEvent[]>(evts => evts.SequenceEqual(new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        }))));
    }

    [Fact]
    public void Quand_un_evenement_est_publie_alors_les_handlers_abonnes_sont_appelles()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new SimpleEventPublisher(eventStore.Object);

        var handlerCalled = false;

        eventPublisher.Subscribe((ArticleAjouteEvt evt) => handlerCalled = true);

        eventPublisher.Publish(IdentiantPanierA, new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        Assert.True(handlerCalled);
    }

    [Fact]
    public void Quand_je_rajoute_un_article_alors_le_panier_est_incremente()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new SimpleEventPublisher(eventStore.Object);

        var paniersQueryHandler = new PaniersQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);
        
        var articleCommandHandler = new PanierCommandHandler(eventStore.Object, eventPublisher);

        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        
        Assert.Equal(new Panier(1), paniersQueryHandler.Get(IdentiantPanierA));
    }
    
    [Fact]
    public void Quand_j_enleve_un_article_alors_le_panier_est_decremente()
    {
        var eventStore = new InMemoryEventStore();
        var eventPublisher = new SimpleEventPublisher(eventStore);

        var paniersQueryHandler = new PaniersQueryHandler(new PaniersInMemoryRepository());
        eventPublisher.Subscribe<ArticleAjouteEvt>(paniersQueryHandler.Quand);
        eventPublisher.Subscribe<ArticleEnleveEvt>(paniersQueryHandler.Quand);
        
        var articleCommandHandler = new PanierCommandHandler(eventStore, eventPublisher);
        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        articleCommandHandler.Handle(new AjouterArticleCmd(IdentiantPanierA, ArticleA));
        
        articleCommandHandler.Handle(new EnleverArticleCmd(IdentiantPanierA, ArticleA));
        
        Assert.Equal(new Panier(2), paniersQueryHandler.Get(IdentiantPanierA));
    }
}

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, IEvent[]> _data;

    public InMemoryEventStore()
    {
        _data = new Dictionary<Guid, IEvent[]>();
    }

    public void Save(Guid aggregateId, IEvent[] events)
    {
        _data[aggregateId] = Get(aggregateId).Concat(events).ToArray();
    }

    public IEvent[] Get(Guid aggregateId)
    {
        return _data.ContainsKey(aggregateId) ? _data[aggregateId] : Array.Empty<IEvent>();
    }
}

public class SimpleEventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<Action<IEvent>>> _handlers;
    private readonly IEventStore _eventStore;

    public SimpleEventPublisher(IEventStore eventStore)
    {
        _handlers = new Dictionary<Type, List<Action<IEvent>>>();
        _eventStore = eventStore;
    }

    public void Publish(Guid aggregateId, IEvent[] events)
    {
        _eventStore.Save(aggregateId, events);

        foreach (var evt in events)
        {
            if (!_handlers.ContainsKey(evt.GetType())) continue;

            _handlers[evt.GetType()].ForEach(handler => handler.Invoke(evt));
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var tHandlers = _handlers.ContainsKey(typeof(T)) ? _handlers[typeof(T)] : new List<Action<IEvent>>();

        tHandlers.Add(x => handler((T) x));

        _handlers[typeof(T)] = tHandlers;
    }
}