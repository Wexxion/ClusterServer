using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public abstract class ClusterClientBase
    {
        protected ClusterClientBase(string[] replicaAddresses)
        {
            ReplicaAddresses = replicaAddresses;
        }

        protected string[] ReplicaAddresses { get; }
        protected abstract ILog Log { get; }

        public abstract Task<string> ProcessRequestAsync(string query, TimeSpan timeout);

        protected Task<string> GetRequestTask(string queryString, bool abort = false)
        {
            var webRequest = CreateRequest(queryString, abort);
            Log.InfoFormat("Processing {0}", webRequest.RequestUri);
            var task = ProcessRequestAsync(webRequest);
            return task;
        }

        protected void AbortAllUncomopletedTasks(Dictionary<Task<string>, string> tasks)
        {
            foreach (var pair in tasks)
                if (!pair.Key.IsCompleted)
                    Task.Run(() => GetRequestTask(pair.Value, abort: true));
        }

        private static HttpWebRequest CreateRequest(string uriStr, bool abort)
        {
            var request = WebRequest.CreateHttp(Uri.EscapeUriString(uriStr));
            request.Proxy = null;
            request.KeepAlive = true;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = 100500;
            request.Headers.Add("abort", $"{abort}");
            return request;
        }

        private async Task<string> ProcessRequestAsync(WebRequest request)
        {
            var timer = Stopwatch.StartNew();
            using (var response = await request.GetResponseAsync())
            {
                var result = await new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEndAsync();
                Log.InfoFormat("Response from {0} received in {1} ms", request.RequestUri, timer.ElapsedMilliseconds);
                return result;
            }
        }
    }
}