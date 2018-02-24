using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        private readonly Random rnd;
        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            rnd = new Random();
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var orderArray = Enumerable.Range(0, ReplicaAddresses.Length).OrderBy(x => rnd.Next()).ToArray();
            var newTimeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / ReplicaAddresses.Length);
            foreach (var i in orderArray)
            {
                var webRequest = CreateRequest(ReplicaAddresses[i] + "?query=" + query);
                Log.InfoFormat("Processing {0}", webRequest.RequestUri);
                var task = ProcessRequestAsync(webRequest);
                if (await Task.WhenAny(task, Task.Delay(newTimeout)) == task)
                    return await task;
            }
            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClusterClient));
    }
}