using System.Text;
using EventStore.Client;
using Newtonsoft.Json;
using static EventStore.Client.Uuid;

namespace TrainingCQRSES.Web.Infra;

public class EventStoreDb : IEventStore
{
    private readonly EventStoreClient _client;

    public EventStoreDb(string cnx)
    {
        _client = new EventStoreClient(EventStoreClientSettings.Create(cnx));
    }

    public async Task Save(IEvent[] events)
    {
        var aggregatesEvents = events.GroupBy(x => x.IdentifiantPanier);

        foreach (var aggregateEvents in aggregatesEvents)
        {
            var streamId = aggregateEvents.Key.ToString();
            var streamEvents = aggregateEvents
                .Select(x => new EventData(NewUuid(), $"{x.GetType().FullName}", SerializeEvent(x)))
                .ToList();

            await _client.AppendToStreamAsync(streamId, StreamState.Any, streamEvents);

            ReadOnlyMemory<byte> SerializeEvent(IEvent evt) =>
                new(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evt, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                })));
        }
    }

    public async Task<IEvent[]> Get(Guid aggregateId)
    {
        var stream = _client.ReadStreamAsync(Direction.Forwards, $"{aggregateId}", StreamPosition.Start);

        if (await stream.ReadState == ReadState.StreamNotFound)
            return Array.Empty<IEvent>();

        return stream
            .ToEnumerable()
            .Select(x => DeserializeEvent(x.Event.Data.Span))
            .ToArray();

        IEvent DeserializeEvent(ReadOnlySpan<byte> data) =>
            JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }) as IEvent;
    }
}