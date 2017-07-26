using JetBrains.Annotations;
using PusherClient.Helper;
using UnityEngine;

namespace PusherClient
{
    public class SampleApp : MonoBehaviour
    {
        private Pusher _pusherClient;
        private Channel _pusherChannel;

        [UsedImplicitly]
        private void Start()
        {
            PusherSettings.LogLevel = PusherSettings.ELogLevel.Error;

            var settings = new PusherSettings("app_key_here", true, "http://auth.url.here");

            _pusherClient = new Pusher(settings);

            _pusherClient.Connected += HandleConnected;
            _pusherClient.ConnectionStateChanged += HandleConnectionStateChanged;
            _pusherClient.Connect();
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            if(_pusherClient != null)
            {
                _pusherClient.Disconnect();
            }
        }

        private Channel SubscribeToChannel(string channelName)
        {
            return _pusherClient.Subscribe(channelName);
        }

        [ContextMenu("Send Message"), UsedImplicitly]
        private void TestSendMessage()
        {
            SendPusherMessageToChannel("player_id", "(insert message here)");
        }

        [UsedImplicitly]
        private void SendPusherMessageToChannel(string playerId, string message)
        {
            _pusherChannel.Trigger("client-send-message", message);
        }

        private void HandleConnected(object sender)
        {
            Debug.Log("Pusher client connected, now subscribing to private channel");
            _pusherChannel = SubscribeToChannel("private-test-channel");
            _pusherChannel.BindAll(HandleChannelEvent);
        }

        private static void HandleChannelEvent(string eventName, object evData)
        {
            Debug.Log("Received event on channel, event name: " + eventName + ", data: " + JsonHelper.Serialize(evData));
        }

        private static void HandleConnectionStateChanged(object sender, ConnectionState state)
        {
            Debug.Log("Pusher connection state changed to: " + state);
        }
    }
}
