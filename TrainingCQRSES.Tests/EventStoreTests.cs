using TrainingCQRSES.Core;
using Xunit;
using static TrainingCQRSES.Tests.Data;

namespace TrainingCQRSES.Tests;

public class EventStoreTests
{
    [Fact]
    public void EventStore_should_return_only_events_for_a_specific_aggregate()
    {
        var eventStore = new InMemoryEventStore();
        
        eventStore.Save(new IEvent[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleB),
        });
        
        eventStore.Save(new IEvent[]
        {
            new ArticleAjouteEvt(IdentiantPanierB, ArticleA),
            new ArticleEnleveEvt(IdentiantPanierB, ArticleA),
            new ArticleAjouteEvt(IdentiantPanierB, ArticleB),
        });

        var events = eventStore.Get(IdentiantPanierA);
        
        Assert.Equal(new IEvent[]
        {
            new ArticleAjouteEvt(IdentiantPanierA, ArticleB),
        }, events);
        
        events = eventStore.Get(IdentiantPanierB);

        Assert.Equal(new IEvent[]
        {
            new ArticleAjouteEvt(IdentiantPanierB, ArticleA),
            new ArticleEnleveEvt(IdentiantPanierB, ArticleA),
            new ArticleAjouteEvt(IdentiantPanierB, ArticleB),
        }, events);
    }

    [Fact]
    public void EventStore_should_throw_an_error_when_version_mismatch()
    {
        var eventStore = new InMemoryEventStore();

        Assert.Throws<VersionMismatchException>(() =>
        {
            eventStore.Save(new []
            {
                new AggregateEvents(new[]
                {
                    new ArticleAjouteEvt(IdentiantPanierA, ArticleB),
                }, 3)
            });
        });
    }
}