namespace TrainingCQRSES.Core;

public class SimpleEventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<Action<IEvent>>> _handlers;
    private readonly IEventStore _eventStore;

    public SimpleEventPublisher(IEventStore eventStore)
    {
        _handlers = new Dictionary<Type, List<Action<IEvent>>>();
        _eventStore = eventStore;
    }

    public async Task Publish(IEvent[] events)
    {
        await _eventStore.Save(events);

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