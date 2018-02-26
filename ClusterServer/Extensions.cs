using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ClusterServer
{
    public static class Extensions
    {
        public static TValue Dequeue<TValue>(this LinkedList<TValue> linkedList)
        {
            var value = linkedList.First.Value;
            linkedList.RemoveFirst();
            return value;
        }

        public static void RemoveAll<TValue>(this LinkedList<TValue> linkedList, Func<TValue, bool> condition)
        {
            foreach (var value in linkedList.Where(condition).ToArray())
                linkedList.Remove(value);
        }

        public static void SendResponse(this HttpListenerContext context, string data)
        {
            context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(data), 0, data.Length);
        }
    }
}