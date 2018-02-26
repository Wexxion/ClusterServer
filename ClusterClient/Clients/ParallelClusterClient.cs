using System;
using System.Collections.Generic;
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
            var tasks = new Dictionary<Task<string>, string>();
            foreach (var uri in ReplicaAddresses)
            {
                var queryString = $"{uri}?query={query}";
                var task = GetRequestTask(queryString);
                tasks.Add(task, queryString);
            }
            var requestsTask = Task.WhenAny(tasks.Keys);
            var first = await Task.WhenAny(requestsTask, Task.Delay(timeout));
            AbortAllUncomopletedTasks(tasks);
            if (first == requestsTask)
                return await await requestsTask;
            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));
    }
}
