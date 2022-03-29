namespace TrainingCQRSES;

public class VersionMismatchException : Exception
{
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