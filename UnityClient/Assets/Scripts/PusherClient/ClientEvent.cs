
namespace PusherClient
{
    internal class PusherEvent
    {
        public const string PusherEventPrefix = "pusher:";

        public const string Error = "pusher:error";
        public const string ConnectionEstablished = "pusher:connection_established";
        public const string ChannelSubscribe = "pusher:subscribe";
        public const string ChannelUnsubscribe = "pusher:unsubscribe";
        public const string ChannelSubscriptionSucceeded = "pusher_internal:subscription_succeeded";
        public const string ChannelSubscriptionError = "pusher_internal:subscription_error";
        public const string ChannelMemberAdded = "pusher_internal:member_added";
        public const string ChannelMemberRemoved = "pusher_internal:member_removed";

        public const string NewMessage = "client-message";
    }
}
