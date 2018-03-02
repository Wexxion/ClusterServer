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
            Helper = new ClientHelper(replicaAddresses);
        }

        protected string[] ReplicaAddresses { get; }
        protected abstract ILog Log { get; }
        public static ClientHelper Helper;

        public abstract Task<string> ProcessRequestAsync(string query, TimeSpan timeout);
        protected Task<string> GetRequestTask(string queryString, bool abort = false)
        {
            var request = CreateRequest(queryString, abort);
            Log.InfoFormat("Processing {0}", request.RequestUri);

            var timer = Stopwatch.StartNew();
            var task = ProcessRequestAsync(request);
            Log.InfoFormat("Response from {0} received in {1} ms", request.RequestUri, timer.ElapsedMilliseconds);

            Helper.AddStatistics(request.Headers["uri"], timer.ElapsedMilliseconds);

            return task;
        }
        public static HttpWebRequest CreateRequest(string uriStr, bool abort)
        {
            var request = WebRequest.CreateHttp(Uri.EscapeUriString(uriStr));
            request.Proxy = null;
            request.KeepAlive = true;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = 100500;
            request.Headers.Add("abort", $"{abort}");
            request.Headers.Add("uri", uriStr.Split('?')[0]);
            return request;
        }

        public static async Task<string> ProcessRequestAsync(WebRequest request)
        {
            using (var response = await request.GetResponseAsync())
                return await new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEndAsync();
        }
    }
}