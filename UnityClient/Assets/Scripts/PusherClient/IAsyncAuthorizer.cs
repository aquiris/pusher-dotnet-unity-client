using System;

namespace PusherClient
{
    public interface IAsyncAuthorizer
    {
        void Authorize(string channelName, string socketId, Action<string, string> onAuthResponse);
    }
}
