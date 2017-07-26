using System;
using System.Net;

namespace PusherClient
{
    public class HttpJsonAuthorizer : IAuthorizer
    {
        private readonly Uri _authEndpoint;

        public HttpJsonAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        public string Authorize(string channelName, string socketId)
        {
            using(var webClient = new WebClient())
            {
                string data = string.Format("{{\"channelName\":\"{0}\",\"socketId\":\"{1}\"}}", channelName, socketId);
                Pusher.Log("Authorize data: " + data);
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";

                string authToken = webClient.UploadString(_authEndpoint, data);
                return authToken;
            }
        }
    }
}
