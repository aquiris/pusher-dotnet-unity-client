using System;
using System.Net;

namespace PusherClient
{
    public class HttpPostAuthorizer : IAuthorizer
    {
        private readonly Uri _authEndpoint;

        public HttpPostAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        public string Authorize(string channelName, string socketId)
        {
            using(var webClient = new WebClient())
            {
                string data = string.Format("channel_name={0}&socket_id={1}", channelName, socketId);
                webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                string authToken = webClient.UploadString(_authEndpoint, "POST", data);
                return authToken;
            }
        }
    }
}
