using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PusherClient.Helper
{
    /**
    * Utilites + thin wrapper around MiniJSON.  If all JSON serializing / deserializing
    * goes through JsonHelper.Serialize / Deserialize then underlying JSON library can
    * be changed more easily
    */

    public static class JsonHelper
    {
        public static List<object> ToList(Vector2 ob)
        {
            var list = new List<object> {ob.x, ob.y};
            return list;
        }

        public static List<object> ToList(Vector3 ob)
        {
            var list = new List<object> {ob.x, ob.y, ob.z};
            return list;
        }

        public static List<object> ToList(Vector4 ob)
        {
            var list = new List<object> {ob.x, ob.y, ob.z, ob.w};
            return list;
        }

        public static List<object> ToList(Quaternion ob)
        {
            var list = new List<object> {ob.x, ob.y, ob.z, ob.w};
            return list;
        }

        public static List<T> ToList<T>(T[] array)
        {
            var list = new List<T>(array);
            return list;
        }

        public static float[] FloatArrayFromDoubleArray(double[] doubleArray)
        {
            var array = new float[doubleArray.Length];
            for(int i = 0; i < doubleArray.Length; i++)
            {
                array[i] = (float) doubleArray[i];
            }
            return array;
        }

        public static T[] ArrayFromList<T>(List<object> list)
        {
            var array = new T[list.Count];
            for(int i = 0; i < list.Count; i++)
            {
                array[i] = (T) list[i];
            }
            return array;
        }

        public static Vector2 Vector2FromList(List<object> list)
        {
            return new Vector2
                       (
                        (float) (double) list[0],
                        (float) (double) list[1]
                       );
        }

        public static Vector3 Vector3FromList(List<object> list)
        {
            return new Vector3
                       (
                        (float) (double) list[0],
                        (float) (double) list[1],
                        (float) (double) list[2]
                       );
        }

        public static Vector4 Vector4FromList(List<object> list)
        {
            return new Vector4
                       (
                        (float) (double) list[0],
                        (float) (double) list[1],
                        (float) (double) list[2],
                        (float) (double) list[3]
                       );
        }

        public static Quaternion QuaternionFromList(List<object> list)
        {
            return new Quaternion
                       (
                        (float) (double) list[0],
                        (float) (double) list[1],
                        (float) (double) list[2],
                        (float) (double) list[3]
                       );
        }

        public static T[] EnumArrayFromList<T>(List<object> list)
        {
            var array = new T[list.Count];
            for(int i = 0; i < list.Count; i++)
            {
                array[i] = EnumFromObject<T>(list[i]);
            }
            return array;
        }

        public static T EnumFromInteger<T>(int index)
        {
            return Enum.IsDefined(typeof(T), index)
                       ? (T) Enum.ToObject(typeof(T), index)
                       : default(T);
        }

        public static T EnumFromObject<T>(object ob)
        {
            return Enum.IsDefined(typeof(T), (string) ob)
                       ? (T) Enum.Parse(typeof(T), (string) ob)
                       : default(T);
        }

        public static List<T> ToTypedList<T>(object ob)
        {
            var objList = ob as List<object>;
            if(objList == null)
            {
                Debug.LogWarning("Attempting to convert " + ob + " into List<object>");
                return new List<T>();
            }

            var resultList = new List<T>();
            for(int i = 0; i < objList.Count; i++)
            {
                if(objList[i] is T)
                {
                    resultList.Add((T) objList[i]);
                }
            }
            return resultList;
        }

        private const string _indentString = "    ";

        public static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for(var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch(ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if(!quoted)
                        {
                            sb.AppendLine();
                            indent++;
                            for(int j = 0; j < indent; j++)
                            {
                                sb.Append(_indentString);
                            }
                        }
                        break;
                    case '}':
                    case ']':
                        if(!quoted)
                        {
                            sb.AppendLine();
                            indent--;
                            for(int j = 0; j < indent; j++)
                            {
                                sb.Append(_indentString);
                            }
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while((index > 0) && (str[--index] == '\\'))
                        {
                            escaped = !escaped;
                        }
                        if(!escaped)
                        {
                            quoted = !quoted;
                        }
                        break;
                    case ',':
                        sb.Append(ch);
                        if(!quoted)
                        {
                            sb.AppendLine();
                            for(int j = 0; j < indent; j++)
                            {
                                sb.Append(_indentString);
                            }
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if(!quoted)
                        {
                            sb.Append(" ");
                        }
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                return (T) Deserialize(json);
            }
            catch(Exception ex)
            {
                Debug.LogWarning("Invalid json, exception while parsing: " + ex.Message);
            }

            return default(T);
        }

        /**
	    * MiniJSON versions of Serialize / Deserialize
	    */

        public static object Deserialize(string json)
        {
            object obj;
            try
            {
                obj = MiniJSON.Json.Deserialize(json);
            }
            catch(Exception)
            {
                obj = null;
            }

            return obj;
        }

        public static string Serialize(object obj)
        {
            return MiniJSON.Json.Serialize(obj);
        }
    }
}
