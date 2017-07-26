using System;
using System.Collections.Generic;

namespace PusherClient.Helper
{
    public static class DataFactoryHelper
    {
        // Utility method for loading string value from dictionary, or default if not set
        public static string GetDictionaryValue(Dictionary<string, object> dictionary, string key,
                                                string defaultValue = "")
        {
            object obj;
            if(dictionary.TryGetValue(key, out obj))
            {
                string stringObj = obj as string;
                if(stringObj != null)
                {
                    return stringObj;
                }

                if(obj != null)
                {
                    return obj.ToString();
                }
            }

            return defaultValue;
        }

        //public static int GetDictionaryInt(Dictionary<string, object> dictionary, string key, int defaultValue)
        //{
        //    string str = GetDictionaryValue(dictionary, key, "null");
        //    int result = defaultValue;
        //    if(str != "null")
        //    {
        //        if(!int.TryParse(str, out result))
        //        {
        //            // Debug.LogWarning( "Failed to parse value as int, going with default.  Value was: '" + str + "', for key: '"+key+"'" );
        //            result = defaultValue;
        //        }
        //    }

        //    return result;
        //}

        //public static double GetDictionaryDouble(Dictionary<string, object> dictionary, string key, double defaultValue)
        //{
        //    if(!dictionary.ContainsKey(key))
        //    {
        //        return defaultValue;
        //    }
        //    else
        //    {
        //        object val = dictionary[key];
        //        if(val is double)
        //        {
        //            return (double) val;
        //        }
        //        else
        //        {
        //            return double.Parse(val.ToString());
        //        }
        //    }
        //}

        //public static bool GetDictionaryBool(Dictionary<string, object> dictionary, string key, bool defaultValue)
        //{
        //    string str = GetDictionaryValue(dictionary, key, "null");
        //    return str != "null" ? bool.Parse(str) : defaultValue;
        //}

        public static T EnumFromString<T>(string str)
        {
            // Enum.Parse will throw all exceptions we need
            var outValue = (T) Enum.Parse(typeof(T), str);
            return outValue;
        }
    }
}
