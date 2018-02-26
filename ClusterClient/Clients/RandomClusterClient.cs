using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RandomClusterClient : ClusterClientBase
    {
        private readonly Random random = new Random();

        public RandomClusterClient(string[] replicaAddresses)
            : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var queryString = $"{ReplicaAddresses[random.Next(ReplicaAddresses.Length)]}?query={query}";
            var task = GetRequestTask(queryString);
            await Task.WhenAny(task, Task.Delay(timeout));
            if (!task.IsCompleted)
                throw new TimeoutException();
            return task.Result;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RandomClusterClient));
    }
}