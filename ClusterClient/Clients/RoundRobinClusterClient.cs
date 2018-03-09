using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClusterClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = new Dictionary<Task<string>, string>();
            var newTimeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / ReplicaAddresses.Length);
            var roundTask = Task.Run(async () =>
            {
                using (Helper.AutoTaskAbort(tasks))
                    while (true)
                    {
                        var address = Helper.GetNextAddress();
                        var queryString = $"{address}?query={query}";
                        var task = GetRequestTask(queryString);
                        tasks.Add(task, queryString);
                        if (await Task.WhenAny(task, Task.Delay(newTimeout)) == task)
                            return await task;
                        Helper.AddToGrayList(address, newTimeout);
                    }
            });
            if (await Task.WhenAny(roundTask, Task.Delay(timeout)) == roundTask)
                return await roundTask;
            throw new TimeoutException();
        }
    }
}