using System;
using System.Threading;
using System.Threading.Tasks;
using Fclp.Internals.Extensions;
using log4net;

namespace ClusterClient.Clients
{
    public class RandomClusterClient : ClusterClientBase
    {
        private readonly Random random = new Random(Thread.CurrentContext.ContextID);

        public RandomClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        protected override ILog Log => LogManager.GetLogger(typeof(RandomClusterClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var address = ReplicaAddresses[random.Next(Helper.ServerСount)];
            var queryString = $"{address}?query={query}";
            var task = GetResultTask(queryString);
            await Task.WhenAny(task, Task.Delay(timeout));
            if (task.IsCompleted && !task.Result.IsNullOrEmpty())
            {
                var res = await task;
                if (!res.IsNullOrEmpty())
                    return res;
            }
            Helper.GrayList.Add(address, timeout);
            throw new TimeoutException();
        }
    }
}