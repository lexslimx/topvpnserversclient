using System;
using System.Collections.Generic;

namespace SurfVpnClientTest1.Services
{
    public class EventAggregator
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();
            _subscribers[eventType].Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (_subscribers.ContainsKey(eventType))
                _subscribers[eventType].Remove(handler);
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var eventType = typeof(TEvent);
            if (_subscribers.ContainsKey(eventType))
            {
                foreach (var handler in _subscribers[eventType])
                {
                    ((Action<TEvent>)handler)?.Invoke(eventData);
                }
            }
        }
    }
}