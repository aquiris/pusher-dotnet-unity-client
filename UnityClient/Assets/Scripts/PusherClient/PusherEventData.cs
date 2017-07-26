using System.Collections.Generic;
using PusherClient.Helper;

namespace PusherClient
{
    internal struct PusherEventData
    {
        public string EventName { get; private set; }
        public string Channel { get; private set; }
        public string Data { get; private set; }

        public static PusherEventData FromJson(string json)
        {
            var data = new PusherEventData();
            var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
            if(dict != null)
            {
                data.EventName = DataFactoryHelper.GetDictionaryValue(dict, PusherJsonKey.Event, string.Empty);
                data.Data = DataFactoryHelper.GetDictionaryValue(dict, PusherJsonKey.Data, string.Empty);
                data.Channel = DataFactoryHelper.GetDictionaryValue(dict, PusherJsonKey.Channel, string.Empty);
            }
            else
            {
                Pusher.LogWarning("Invalid PusherEventData: '" + json + "'");
            }

            return data;
        }
    }
}
