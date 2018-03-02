using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class SmartClusterClient : ClusterClientBase
    {
        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan totaltimeout)
        {
            var tasks = new Dictionary<Task<string>, string>();
            var newTimeout = TimeSpan.FromMilliseconds(totaltimeout.TotalMilliseconds / ReplicaAddresses.Length);

            using (Helper.AutoTaskAbort(tasks))
            {
                for (var i = 0; i < Helper.ServerСount; i++)
                {
                    var address = Helper.GetNextAddress();
                    var queryString = $"{address}?query={query}";
                    var task = GetRequestTask(queryString);
                    var delayTask = GetDelayStringTask(newTimeout);

                    tasks.Add(task, queryString);
                    tasks.Add(delayTask, null);

                    var first = await Task.WhenAny(tasks.Keys);
                    tasks.Remove(delayTask);
                    if (first != delayTask)
                    {
                        if (first != task)
                            Helper.AddToGrayList(address, newTimeout);
                        return await first;
                    }
                }

                throw new TimeoutException();
            }
        }

        private async Task<string> GetDelayStringTask(TimeSpan delay)
        {
            await Task.Delay(delay);
            return null;
        }
    }
}