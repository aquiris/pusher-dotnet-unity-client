using System;
using System.Collections.Generic;
using System.Net;

namespace PusherClient
{
    public class HttpAsyncJsonAuthorizer : IAsyncAuthorizer
    {
        private struct AuthResponse
        {
            public string ChannelName;
            public Action<string, string> OnAuthResponse;
        }

        private readonly Uri _authEndpoint;

        private readonly Dictionary<WebClient, AuthResponse> _responseMap;

        public HttpAsyncJsonAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
            _responseMap = new Dictionary<WebClient, AuthResponse>();
        }

        public void Authorize(string channelName, string socketId, Action<string, string> onAuthResponse)
        {
            var authResponseObj = new AuthResponse {ChannelName = channelName, OnAuthResponse = onAuthResponse};

            string data = string.Format("{{\"channelName\":\"{0}\",\"socketId\":\"{1}\"}}", channelName, socketId);
            Pusher.Log("Authorize data: " + data);

            var webClient = new WebClient();

            _responseMap.Add(webClient, authResponseObj);

            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
            webClient.UploadStringCompleted += OnUploadStringCompleted;
            webClient.UploadStringAsync(_authEndpoint, data);
        }

        private void OnUploadStringCompleted(object sender, UploadStringCompletedEventArgs eventArgs)
        {
            var webclient = (WebClient) sender;
            webclient.UploadStringCompleted -= OnUploadStringCompleted;

            AuthResponse authResponseObj = _responseMap[webclient];
            _responseMap.Remove(webclient);
            webclient.Dispose();

            Pusher.Log("Got reply from server auth: " + authResponseObj.OnAuthResponse);

            if(authResponseObj.OnAuthResponse != null)
            {
                authResponseObj.OnAuthResponse(authResponseObj.ChannelName, eventArgs.Result);
            }
        }
    }
}
