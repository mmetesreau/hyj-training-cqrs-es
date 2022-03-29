using System.Text;
using EventStore.Client;
using Newtonsoft.Json;

namespace TrainingCQRSES.Core;

public class VersionMismatchException : Exception
{
}

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
            var streamEvents = aggregateEvents.Select(x =>
                    new EventData(
                        Uuid.NewUuid(),
                        $"{x.GetType().FullName}",
                        new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All
                            })))))
                .ToList();

            await _client.AppendToStreamAsync(streamId, StreamState.Any, streamEvents);
        }
    }

    public async Task<IEvent[]> Get(Guid aggregateId)
    {
        var stream = _client.ReadStreamAsync(Direction.Forwards, $"{aggregateId}", StreamPosition.Start);

        if (await stream.ReadState == ReadState.StreamNotFound)
            return Array.Empty<IEvent>();

        return stream
            .ToEnumerable()
            .Select(x => JsonConvert.DeserializeObject(Encoding.UTF8.GetString(x.Event.Data.Span), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }))
            .Cast<IEvent>()
            .ToArray();
    }
}

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<IEvent>> _data;

    public InMemoryEventStore()
    {
        _data = new Dictionary<Guid, List<IEvent>>();
    }

    public Task Save(IEvent[] events)
    {
        foreach (var evt in events)
        {
            if (!_data.ContainsKey(evt.IdentifiantPanier))
                _data[evt.IdentifiantPanier] = new List<IEvent>();

            _data[evt.IdentifiantPanier].Add(evt);
        }

        return Task.CompletedTask;
    }

    public async Task Save(AggregateEvents[] aggregatesEvents)
    {
        foreach (var aggregateEvents in aggregatesEvents)
        {
            if (!aggregateEvents.Events.Any()) return;

            var stream = await Get(aggregateEvents.Events.First().IdentifiantPanier);

            if (stream.Length != aggregateEvents.Version)
                throw new VersionMismatchException();

            await Save(aggregateEvents.Events);
        }
    }

    public Task<IEvent[]> Get(Guid aggregateId)
    {
        var events = _data.ContainsKey(aggregateId) ? _data[aggregateId].ToArray() : Array.Empty<IEvent>();

        return Task.FromResult(events);
    }
}

public class AggregateIdMismatchException : Exception
{
}

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