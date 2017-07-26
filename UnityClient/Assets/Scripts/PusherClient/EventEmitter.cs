using System;
using System.Collections.Generic;
using PusherClient.Helper;

namespace PusherClient
{
    public class EventEmitter
    {
        private readonly Dictionary<string, List<Action<object>>> _eventListeners;
        private readonly List<Action<string, object>> _generalListeners;

        public EventEmitter()
        {
            _eventListeners = new Dictionary<string, List<Action<object>>>();
            _generalListeners = new List<Action<string, object>>();
        }

        public void Bind(string eventName, Action<object> listener)
        {
            List<Action<object>> eventListeners;
            if(_eventListeners.TryGetValue(eventName, out eventListeners))
            {
                eventListeners.Add(listener);
            }
            else
            {
                eventListeners = new List<Action<object>> {listener};
                _eventListeners.Add(eventName, eventListeners);
            }
        }

        public void BindAll(Action<string, object> listener)
        {
            _generalListeners.Add(listener);
        }

        internal void EmitEvent(string eventName, string data)
        {
            var obj = JsonHelper.Deserialize<object>(data);

            // Emit to general listeners regardless of event type
            for(int i = 0; i < _generalListeners.Count; i++)
            {
                if(_generalListeners[i] != null)
                {
                    _generalListeners[i](eventName, obj);
                }
            }

            List<Action<object>> listeners;
            if(_eventListeners.TryGetValue(eventName, out listeners))
            {
                for(int i = 0; i < listeners.Count; i++)
                {
                    if(listeners[i] != null)
                    {
                        listeners[i](obj);
                    }
                }
            }
        }
    }
}
