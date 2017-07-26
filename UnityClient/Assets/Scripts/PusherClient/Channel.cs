using System.Collections;

namespace PusherClient
{
    public delegate void SubscriptionEventHandler(object sender);

    public class Channel : EventEmitter
    {
        private readonly Pusher _pusher;

        public event SubscriptionEventHandler OnSubscription;
        public string Name { get; private set; }

        public bool IsSubscribed { get; private set; }

        public Channel(Pusher pusher, string channelName)
        {
            _pusher = pusher;

            Name = channelName;
        }

        internal virtual void SubscriptionSucceeded(string data)
        {
            IsSubscribed = true;

            if(OnSubscription != null)
            {
                OnSubscription(this);
            }
        }

        public void Unsubscribe()
        {
            IsSubscribed = false;

            _pusher.Unsubscribe(Name);
        }

        public void Trigger(string eventName, object obj)
        {
            _pusher.Trigger(Name, eventName, obj);
        }

        public void SendMessage(string message)
        {
            _pusher.Trigger(Name, PusherEvent.NewMessage, new DictionaryEntry(PusherJsonKey.Message, message));
        }
    }
}
