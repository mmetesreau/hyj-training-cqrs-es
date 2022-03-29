using System.Linq;
using Moq;
using TrainingCQRSES;
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
