using System;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RandomClusterClient : ClusterClientBase
    {
        private readonly Random random = new Random();

        public RandomClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        protected override ILog Log => LogManager.GetLogger(typeof(RandomClusterClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var address = ReplicaAddresses[random.Next(Helper.ServerСount)];
            var queryString = $"{address}?query={query}";
            var task = GetRequestTask(queryString);
            await Task.WhenAny(task, Task.Delay(timeout));
            if (task.IsCompleted)
                return task.Result;
            Helper.AddToGrayList(address, timeout);
            throw new TimeoutException();
            
        }
    }
}