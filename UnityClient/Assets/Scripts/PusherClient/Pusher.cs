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
        public static void Log(string message)
        {
            if((PusherSettings.LogLevel & PusherSettings.ELogLevel.Log) == PusherSettings.ELogLevel.Log)
            {
                UnityEngine.Debug.Log("[Pusher] " + message);
            }
        }

        public static void LogWarning(string message)
        {
            if((PusherSettings.LogLevel & PusherSettings.ELogLevel.Warning) == PusherSettings.ELogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning("[Pusher] " + message);
            }
        }

        public static void LogError(string message)
        {
            if((PusherSettings.LogLevel & PusherSettings.ELogLevel.Error) == PusherSettings.ELogLevel.Error)
            {
                UnityEngine.Debug.LogError("[Pusher] " + message);
            }
        }

        public const string Host = "ws-us2.pusher.com";

        private const int _protocolNumber = 5;

        private readonly PusherSettings _settings;
        private readonly IAuthorizer _authorizer;
        private Connection _connection;

        public event ConnectedEventHandler Connected;
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;

        #region Properties

        public Dictionary<string, Channel> Channels { get; private set; }

        public string SocketID
        {
            get { return _connection.SocketID; }
        }

        public ConnectionState State
        {
            get { return _connection.State; }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="Pusher" /> class.
        /// </summary>
        public Pusher(PusherSettings settings)
        {
            if(string.IsNullOrEmpty(settings.HttpAuthUrl))
            {
                throw new PusherException("Missing PusherSetting endpoints.", ErrorCodes.ConnectionFailed);
            }

            Channels = new Dictionary<string, Channel>();

            _settings = settings;
            _authorizer = new HttpJsonAuthorizer(settings.HttpAuthUrl);
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

        public Channel Subscribe(string channelName)
        {
            if(_connection.State != ConnectionState.Connected)
            {
                LogWarning("You must wait for Pusher to connect before you can subscribe to a channel");
            }

            if(Channels.ContainsKey(channelName))
            {
                LogWarning("Channel '" + channelName +
                           "' is already subscribed to. Subscription event has been ignored.");
                return Channels[channelName];
            }

            // If private or presence channel, check that auth endpoint has been set
            var channelType = ChannelTypes.Public;

            if(channelName.StartsWith("private-", StringComparison.OrdinalIgnoreCase))
            {
                channelType = ChannelTypes.Private;
            }
            else if(channelName.StartsWith("presence-", StringComparison.OrdinalIgnoreCase))
            {
                channelType = ChannelTypes.Presence;
            }

            return SubscribeToChannel(channelType, channelName);
        }

        private Channel SubscribeToChannel(ChannelTypes type, string channelName)
        {
            switch(type)
            {
                case ChannelTypes.Public:
                    Channels.Add(channelName, new Channel(this, channelName));
                    break;
                case ChannelTypes.Private:
                    AuthEndpointCheck();
                    Channels.Add(channelName, new PrivateChannel(this, channelName));
                    break;
                case ChannelTypes.Presence:
                    AuthEndpointCheck();
                    LogWarning("Presence Channels are not implemented yet.");
                    Channels.Add(channelName, new PresenceChannel(this, channelName));
                    break;
            }

            switch(type)
            {
                case ChannelTypes.Presence:
                case ChannelTypes.Private:
                    Log("Calling auth for channel for: " + channelName);
                    AuthorizeChannel(channelName);
                    break;

                // No need for auth details. Just send subscribe event
                default:
                    var channelData = new Dictionary<string, object> {{PusherJsonKey.Channel, channelName}};
                    string json = JsonHelper.Serialize(new Dictionary<string, object>
                                                       {
                                                           {PusherJsonKey.Event, PusherEvent.ChannelSubscribe},
                                                           {PusherJsonKey.Data, channelData}
                                                       });
                    _connection.Send(json);
                    break;
            }

            return Channels[channelName];
        }

        private void AuthorizeChannel(string channelName)
        {
            string authJson = _authorizer.Authorize(channelName, _connection.SocketID);
            Log("Got reply from server auth: " + authJson);
            SendChannelAuthData(channelName, authJson);

            // TODO: SEND CHANNEL DATA WHEN PRESENCE CHANNEl
        }

        private void SendChannelAuthData(string channelName, string jsonAuth)
        {
            // parse info from json data
            var authDict = JsonHelper.Deserialize<Dictionary<string, object>>(jsonAuth);
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

        private void AuthEndpointCheck()
        {
            if(_authorizer == null)
            {
                const string message = "You must set a ChannelAuthorizer property to use private or presence channels";
                throw new PusherException(message, ErrorCodes.ChannelAuthorizerNotSet);
            }
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
        }

        private void OnConnectionStateChanged(object sender, ConnectionState state)
        {
            if(ConnectionStateChanged != null)
            {
                ConnectionStateChanged(sender, state);
            }
        }

        private void OnConnected(object sender)
        {
            if(Connected != null)
            {
                Connected(sender);
            }
        }
    }
}
