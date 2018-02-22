using System;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    class ParallelClusterClient : ClusterClientBase
    {
        public ParallelClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = ReplicaAddresses.Select(async uri =>
            {
                var webRequest = CreateRequest(uri + "?query=" + query);
                Log.InfoFormat("Processing {0}", webRequest.RequestUri);
                var task = ProcessRequestAsync(webRequest);
                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    return await task;
                throw new TimeoutException();
            });
            var first = await Task.WhenAny(tasks);
            return await first;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));
    }
}
