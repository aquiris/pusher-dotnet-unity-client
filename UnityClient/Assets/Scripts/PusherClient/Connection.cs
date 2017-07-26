using System;
using System.Collections.Generic;
using PusherClient.Helper;
using WebSocketSharp;

namespace PusherClient
{
    internal class Connection
    {
        private readonly Pusher _pusher;
        private readonly string _url;

        private WebSocket _websocket;

        private bool _allowReconnect = true;

        internal event ConnectedEventHandler Connected;
        internal event ConnectionStateChangedEventHandler ConnectionStateChanged;

        #region Properties
        internal string SocketID { get; private set; }
        internal ConnectionState State { get; private set; }
        #endregion

        internal Connection(Pusher pusher, string url)
        {
            State = ConnectionState.Initialized;
            _url = url;
            _pusher = pusher;
        }

        internal void Connect()
        {
            // TODO: Handle and test disconnection / errors etc
            // TODO: Add 'connecting_in' event

            ChangeState(ConnectionState.Connecting);
            _allowReconnect = true;

            _websocket = new WebSocket(_url);
            _websocket.OnError += WebsocketError;
            _websocket.OnOpen += WebsocketOpened;
            _websocket.OnClose += WebsocketClosed;
            _websocket.OnMessage += WebsocketMessageReceived;
            _websocket.ConnectAsync();
        }

        internal void Disconnect()
        {
            _allowReconnect = false;
            _websocket.Close();
            ChangeState(ConnectionState.Disconnected);
        }

        internal void Send(string message)
        {
            Pusher.Log("Sending: " + message);
            _websocket.SendAsync(message, delegate { });
        }

        private void ChangeState(ConnectionState state)
        {
            State = state;

            if(ConnectionStateChanged != null)
            {
                ConnectionStateChanged(this, State);
            }
        }

        private void WebsocketMessageReceived(object sender, MessageEventArgs e)
        {
            Pusher.Log("Websocket message received: " + e.Data);

            PusherEventData message = PusherEventData.FromJson(e.Data);
            _pusher.EmitEvent(message.EventName, message.Data);

            if(message.EventName.StartsWith(PusherEvent.PusherEventPrefix))
            {
                HandlePusherEvent(e, message);
            }
            else
            {
                // Assume it's a channel event
                HandleChannelEvent(message);
            }
        }

        private void HandleChannelEvent(PusherEventData message)
        {
            Channel channel;
            if(_pusher.Channels.TryGetValue(message.Channel, out channel))
            {
                channel.EmitEvent(message.EventName, message.Data);
            }
        }

        private void HandlePusherEvent(MessageEventArgs args, PusherEventData message)
        {
            // Assume Pusher event
            switch(message.EventName)
            {
                case PusherEvent.Error:
                    ParseError(message.Data);
                    break;

                case PusherEvent.ConnectionEstablished:
                    ParseConnectionEstablished(message.Data);
                    break;

                case PusherEvent.ChannelSubscriptionSucceeded:
                    {
                        Channel channel;
                        if(_pusher.Channels.TryGetValue(message.Channel, out channel))
                        {
                            channel.SubscriptionSucceeded(message.Data);
                        }
                        break;
                    }

                case PusherEvent.ChannelSubscriptionError:
                    throw new PusherException("Error received on channel subscriptions: " + args.Data,
                                              ErrorCodes.SubscriptionError);

                // Assume channel event
                case PusherEvent.ChannelMemberAdded:
                    {
                        Channel channel;
                        if(_pusher.Channels.TryGetValue(message.Channel, out channel))
                        {
                            var presenceChannel = channel as PresenceChannel;
                            if(presenceChannel != null)
                            {
                                presenceChannel.AddMember(message.Data);
                                break;
                            }
                        }
                        Pusher.LogWarning("Received a presence event on channel '" + message.Channel +
                                          "', however there is no presence channel which matches.");
                        break;
                    }

                // Assume channel event
                case PusherEvent.ChannelMemberRemoved:
                    {
                        Channel channel;
                        if(_pusher.Channels.TryGetValue(message.Channel, out channel))
                        {
                            var presenceChannel = channel as PresenceChannel;
                            if(presenceChannel != null)
                            {
                                presenceChannel.RemoveMember(message.Data);
                                break;
                            }
                        }

                        Pusher.LogWarning("Received a presence event on channel '" + message.Channel +
                                          "', however there is no presence channel which matches.");
                        break;
                    }
            }
        }

        private static void WebsocketOpened(object sender, EventArgs e)
        {
            Pusher.Log("Websocket opened successfully.");
        }

        private void WebsocketClosed(object sender, EventArgs e)
        {
            Pusher.Log("Websocket connection has been closed");

            ChangeState(ConnectionState.Disconnected);

            if(_allowReconnect)
            {
                Connect();
            }
        }

        private static void WebsocketError(object sender, ErrorEventArgs e)
        {
            // TODO: What happens here? Do I need to re-connect, or do I just log the issue?
            Pusher.LogWarning("Websocket error: " + e.Message);
        }

        private void ParseConnectionEstablished(string data)
        {
            var dict = JsonHelper.Deserialize<Dictionary<string, object>>(data);

            SocketID = DataFactoryHelper.GetDictionaryValue(dict, PusherJsonKey.SocketID, string.Empty);

            ChangeState(ConnectionState.Connected);

            if(Connected != null)
                Connected(this);
        }

        private static void ParseError(string data)
        {
            var dict = JsonHelper.Deserialize<Dictionary<string, object>>(data);
            string message = DataFactoryHelper.GetDictionaryValue(dict, PusherJsonKey.Message, string.Empty);
            string errorCodeStr = DataFactoryHelper.GetDictionaryValue(dict, PusherJsonKey.Code,
                                                                       ErrorCodes.Unknown.ToString());

            var error = DataFactoryHelper.EnumFromString<ErrorCodes>(errorCodeStr);

            throw new PusherException(message, error);
        }
    }
}
