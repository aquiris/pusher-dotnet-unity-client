using System.Collections.Generic;

namespace PusherClient
{
    public delegate void MemberEventHandler(object sender);

    public class PresenceChannel : PrivateChannel
    {
        public Dictionary<string, object> Members { get; private set; }

        public event MemberEventHandler MemberAdded;
        public event MemberEventHandler MemberRemoved;

        public PresenceChannel(Pusher pusher, string channelName) : base(pusher, channelName)
        {
            Members = new Dictionary<string, object>();
        }

        internal override void SubscriptionSucceeded(string data)
        {
            Members = new Dictionary<string, object>(); //ParseMembersList(data);
            base.SubscriptionSucceeded(data);
        }

        internal void AddMember(string data)
        {
            var member = new KeyValuePair<string, object>("", ""); //ParseMember(data);

            if(Members.ContainsKey(member.Key))
            {
                Members[member.Key] = member.Value;
            }
            else
            {
                Members.Add(member.Key, member.Value);
            }

            if(MemberAdded != null)
            {
                MemberAdded(this);
            }
        }

        internal void RemoveMember(string data)
        {
            var member = new KeyValuePair<string, object>("", ""); //ParseMember(data);

            if(Members.ContainsKey(member.Key))
            {
                Members.Remove(member.Key);

                if(MemberRemoved != null)
                {
                    MemberRemoved(this);
                }
            }
        }

        /*
        private Dictionary<string, object> ParseMembersList(string data)
        {
            Dictionary<string, object> dataAsDict = JsonHelper.Deserialize<Dictionary<string, object>>(data);
            Dictionary<string, object> presenceDict = (Dictionary<string,object>)dataAsDict[ "presence" ];
            foreach( KeyValuePair presenceKvp in dataAsDict ) {
                string 
                i++;
            }

            for (int i = 0; i < (int)dataAsObj.presence.count; i++)
            {
                var id = (string)dataAsObj.presence.ids[i];
                var val = (dynamic)dataAsObj.presence.hash[id];
                members.Add(id, val);
            }

            return members;
        }

        private KeyValuePair<string, object> ParseMember(string data)
        {
            var dataAsObj = JsonHelper.Deserialize<dynamic>(data);

            var id = (string)dataAsObj.user_id;
            var val = (dynamic)dataAsObj.user_info;

            return new KeyValuePair<string, dynamic>(id, val);
        }
        */
    }
}
