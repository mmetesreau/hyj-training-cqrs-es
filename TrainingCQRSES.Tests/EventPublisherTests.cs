using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using TrainingCQRSES.Core;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class EventPublisherTests
{
    [Fact]
    public void Quand_un_evenement_est_publie_alors_il_est_persiste()
    {
        var eventStore = new Mock<IEventStore>();
        var eventPublisher = new SimpleEventPublisher(eventStore.Object);

        eventPublisher.Publish(new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        eventStore.Verify(x => x.Save(It.Is<IEvent[]>(evts => evts.SequenceEqual(new[]
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

        eventPublisher.Publish(new[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleA)
        });

        Assert.True(handlerCalled);
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

    public void Publish(IEvent[] events)
    {
        _eventStore.Save(events);

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
