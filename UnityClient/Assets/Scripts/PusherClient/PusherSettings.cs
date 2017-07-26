using System;

namespace PusherClient
{
    public struct PusherSettings
    {
        [Flags]
        public enum ELogLevel
        {
            None = 0,
            Log = 1 << 0,
            Warning = (1 << 1) | Log,
            Error = (1 << 2) | Warning
        }

        // Client name & version for identifying client library
        public const string ClientName = "client-pusherUnityAquirisFork";
        public const string ClientVersion = "1.0";

        // Defines the log level in Pusher
        public static ELogLevel LogLevel = ELogLevel.Log;

        // App Key from pusher.com app settings
        public string AppKey { get; private set; }

        // If true, then connection to pusher will be encrypted
        public bool Encrypted { get; private set; }

        // If specified, then this will be used as callback url for authorizing connections to private channels
        public string HttpAuthUrl { get; private set; }

        public PusherSettings(string appKey, bool encrypted, string httpAuthUrl = "") : this()
        {
            AppKey = appKey;
            Encrypted = encrypted;
            HttpAuthUrl = httpAuthUrl;
        }
    }
}
