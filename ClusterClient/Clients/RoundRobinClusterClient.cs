using System;
using System.Collections.Generic;
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
            var tasks = new Dictionary<Task<string>, string>();
            var orderArray = Enumerable.Range(0, ReplicaAddresses.Length).OrderBy(x => rnd.Next()).ToArray();
            var newTimeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / ReplicaAddresses.Length);

            foreach (var i in orderArray)
            {
                var queryString = $"{ReplicaAddresses[i]}?query={query}";
                var task = GetRequestTask(queryString);
                tasks.Add(task, queryString);
                if (await Task.WhenAny(task, Task.Delay(newTimeout)) != task) continue;
                AbortAllUncomopletedTasks(tasks);
                return await task;
            }
            AbortAllUncomopletedTasks(tasks);
            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClusterClient));
    }
}