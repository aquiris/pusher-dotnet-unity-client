using System;
using System.Collections.Generic;
using System.Text;
using PusherClient.Helper;

namespace PusherClient
{
    // A delegate type for hooking up change notifications.
    public delegate void ConnectedEventHandler(object sender);

    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionState state);

    public class Pusher : EventEmitter
    {
        public const string Host = "ws-us2.pusher.com";

        private const int _protocolNumber = 5;

        private readonly IAsyncAuthorizer _authorizer;

        private readonly PusherSettings _settings;

        private Connection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pusher" /> class.
        /// </summary>
        public Pusher(PusherSettings settings)
        {
            if(string.IsNullOrEmpty(settings.HttpAuthUrl))
            {
                throw new PusherException("Missing PusherSetting endpoint", ErrorCodes.ConnectionFailed);
            }

            Channels = new Dictionary<string, Channel>();

            _settings = settings;
            _authorizer = new HttpAsyncJsonAuthorizer(settings.HttpAuthUrl);
        }

        public event ConnectedEventHandler Connected;

        public event ConnectionStateChangedEventHandler ConnectionStateChanged;

        public static void Log(string message)
        {
            if((PusherSettings.LogLevel & PusherSettings.ELogLevel.Log) != 0)
            {
                UnityEngine.Debug.Log("[Pusher] " + message);
            }
        }

        public static void LogError(string message)
        {
            if((PusherSettings.LogLevel & PusherSettings.ELogLevel.Error) != 0)
            {
                UnityEngine.Debug.LogError("[Pusher] " + message);
            }
        }

        public static void LogWarning(string message)
        {
            if((PusherSettings.LogLevel & PusherSettings.ELogLevel.Warning) != 0)
            {
                UnityEngine.Debug.LogWarning("[Pusher] " + message);
            }
        }

        public Dictionary<string, Channel> Channels { get; private set; }

        public string SocketID
        {
            get { return _connection == null ? null : _connection.SocketID; }
        }

        public ConnectionState State
        {
            get { return _connection == null ? ConnectionState.Disconnected : _connection.State; }
        }

        public void Connect()
        {
            // Check current connection state
            if(_connection != null)
            {
                switch(_connection.State)
                {
                    case ConnectionState.Connected:
                        LogWarning("Attempt to connect when connection is already in 'Connected' state. " +
                                   "New attempt has been ignored.");
                        return;
                    case ConnectionState.Connecting:
                        LogWarning("Attempt to connect when connection is already in 'Connecting' state. " +
                                   "New attempt has been ignored.");
                        return;
                    case ConnectionState.Failed:
                        LogWarning("Cannot attempt re-connection once in 'Failed' state");
                        throw new PusherException("Cannot attempt re-connection once in 'Failed' state",
                                                  ErrorCodes.ConnectionFailed);
                }
            }

            string scheme = _settings.Encrypted
                                ? "wss://"
                                : "ws://";

            // TODO: Fallback to secure?

            var urlBuilder = new StringBuilder();
            urlBuilder.AppendFormat("{0}{1}/app/{2}?protocol={3}&client={4}&version={5}",
                                    scheme, Host, _settings.AppKey, _protocolNumber,
                                    PusherSettings.ClientName, PusherSettings.ClientVersion);

            string builtUrl = urlBuilder.ToString();

            Log("Connecting to url: '" + builtUrl + "'");

            _connection = new Connection(this, builtUrl);
            _connection.Connected += OnConnected;
            _connection.ConnectionStateChanged += OnConnectionStateChanged;
            _connection.Connect();
        }

        public void Disconnect()
        {
            _connection.Disconnect();
        }

        public void Send(string eventName, object data, string channelName = null)
        {
            string json = JsonHelper.Serialize(new Dictionary<string, object>
                                               {
                                                   {PusherJsonKey.Event, eventName},
                                                   {PusherJsonKey.Data, data},
                                                   {PusherJsonKey.Channel, channelName}
                                               });
            _connection.Send(json);
        }

        public Channel Subscribe(string channelName)
        {
            if(_connection.State != ConnectionState.Connected)
            {
                LogWarning("You must wait for Pusher to connect before you can subscribe to a channel");
                return null;
            }

            if(Channels.ContainsKey(channelName))
            {
                Log("Channel '" + channelName + "' is already subscribed to.");
                return Channels[channelName];
            }

            // If private or presence channel, check that auth endpoint has been set
            var channelType = EChannelType.Public;

            if(channelName.StartsWith("private-", StringComparison.OrdinalIgnoreCase))
            {
                channelType = EChannelType.Private;
            }
            else if(channelName.StartsWith("presence-", StringComparison.OrdinalIgnoreCase))
            {
                channelType = EChannelType.Presence;
            }

            return SubscribeToChannel(channelType, channelName);
        }

        internal void Trigger(string channelName, string eventName, object obj)
        {
            string json = JsonHelper.Serialize(new Dictionary<string, object>()
                                               {
                                                   {PusherJsonKey.Event, eventName},
                                                   {PusherJsonKey.Data, obj},
                                                   {PusherJsonKey.Channel, channelName}
                                               });
            _connection.Send(json);
        }

        internal void Unsubscribe(string channelName)
        {
            var unsubscribeData = new Dictionary<string, object> {{PusherJsonKey.Channel, channelName}};
            string json = JsonHelper.Serialize(new Dictionary<string, object>()
                                               {
                                                   {PusherJsonKey.Event, PusherEvent.ChannelUnsubscribe},
                                                   {PusherJsonKey.Data, unsubscribeData}
                                               });
            _connection.Send(json);

            Channels.Remove(channelName);
        }

        private void OnConnected(object sender)
        {
            if(Connected != null)
            {
                Connected(sender);
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionState state)
        {
            if(ConnectionStateChanged != null)
            {
                ConnectionStateChanged(sender, state);
            }
        }

        private Channel SubscribeToChannel(EChannelType channelType, string channelName)
        {
            switch(channelType)
            {
                case EChannelType.Public:
                    Channels.Add(channelName, new Channel(this, channelName));
                    SendPublicSubscription(channelName);
                    break;
                case EChannelType.Private:
                    Channels.Add(channelName, new PresenceChannel(this, channelName));
                    SendAuthenticatedSubscription(channelName);
                    break;
                case EChannelType.Presence:
                    Channels.Add(channelName, new PresenceChannel(this, channelName));
                    SendAuthenticatedSubscription(channelName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("channelType", channelType, "Invalid channel type");
            }

            return Channels[channelName];
        }

        private void SendPublicSubscription(string channelName)
        {
            var channelData = new Dictionary<string, object> {{PusherJsonKey.Channel, channelName}};
            string json = JsonHelper.Serialize(new Dictionary<string, object>
                                               {
                                                   {PusherJsonKey.Event, PusherEvent.ChannelSubscribe},
                                                   {PusherJsonKey.Data, channelData}
                                               });
            _connection.Send(json);
        }

        private void SendAuthenticatedSubscription(string channelName)
        {
            _authorizer.Authorize(channelName, _connection.SocketID, OnAuthenticationResponse);
        }

        private void OnAuthenticationResponse(string channelName, string authResponse)
        {
            var authDict = JsonHelper.Deserialize<Dictionary<string, object>>(authResponse);
            string authFromMessage = DataFactoryHelper.GetDictionaryValue(authDict, PusherJsonKey.Auth, string.Empty);
            var channelAuthData = new Dictionary<string, object>
                                  {
                                      {PusherJsonKey.Channel, channelName},
                                      {PusherJsonKey.Auth, authFromMessage}
                                  };

            //string channelDataFromMessage = DataFactoryHelper.GetDictionaryValue( authDict, "channel_data", string.Empty );

            string json = JsonHelper.Serialize(new Dictionary<string, object>
                                               {
                                                   {PusherJsonKey.Event, PusherEvent.ChannelSubscribe},
                                                   {PusherJsonKey.Data, channelAuthData}

                                                   // TODO: SEND CHANNEL DATA WHEN PRESENCE CHANNEL
                                                   // { "channel_data", channelDataFromMessage }
                                               });

            _connection.Send(json);
        }
    }
}
