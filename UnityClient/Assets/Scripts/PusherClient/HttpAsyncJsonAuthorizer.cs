using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

            ServicePointManager.ServerCertificateValidationCallback += CustomServerCertificateValidationCallback;
        }

        ~HttpAsyncJsonAuthorizer()
        {
            ServicePointManager.ServerCertificateValidationCallback -= CustomServerCertificateValidationCallback;
        }

        public void Authorize(string channelName, string socketId, Action<string, string> onAuthResponse)
        {
            var authResponseObj = new AuthResponse {ChannelName = channelName, OnAuthResponse = onAuthResponse};

            string data = string.Format(@"{{""channelName"":""{0}"", ""socketId"":""{1}""}}", channelName, socketId);
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

            Pusher.Log("Got reply from server auth: " + eventArgs.Result);

            if(authResponseObj.OnAuthResponse != null)
            {
                authResponseObj.OnAuthResponse(authResponseObj.ChannelName, eventArgs.Result);
            }
        }

        private static bool CustomServerCertificateValidationCallback(object sender,
                                                                      X509Certificate certificate, X509Chain chain,
                                                                      SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;

            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if(sslPolicyErrors != SslPolicyErrors.None)
            {
                for(int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if(chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        continue;
                    }
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2) certificate);
                    if(!chainIsValid)
                    {
                        isOk = false;
                        break;
                    }
                }
            }
            return isOk;
        }
    }
}
