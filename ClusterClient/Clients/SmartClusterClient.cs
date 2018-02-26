using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class SmartClusterClient : ClusterClientBase
    {
        private readonly Random rnd;
        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            rnd = new Random();
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan totaltimeout)
        {   
            var tasks = new Dictionary<Task<string>, string>();

            var roundTask = StartRound(tasks, query, totaltimeout);
            var first = await Task.WhenAny(roundTask, Task.Delay(totaltimeout));
            AbortAllUncomopletedTasks(tasks);
            if (first == roundTask)
                return await roundTask;
            throw new TimeoutException();
        }

        private async Task<string> GetDelayStringTask(TimeSpan delay)
        {
            await Task.Delay(delay);
            return null;
        }

        private async Task<string> StartRound(Dictionary<Task<string>, string> tasks, string query, TimeSpan totaltimeout)
        {
            var orderArray = Enumerable.Range(0, ReplicaAddresses.Length).OrderBy(x => rnd.Next()).ToArray();
            var newTimeout = TimeSpan.FromMilliseconds(totaltimeout.TotalMilliseconds / ReplicaAddresses.Length);

            foreach (var i in orderArray)
            {
                var queryString = $"{ReplicaAddresses[i]}?query={query}";
                var task = GetRequestTask(queryString);
                var delayTask = GetDelayStringTask(newTimeout);

                tasks.Add(task, queryString);
                tasks.Add(delayTask, null);

                var first = await Task.WhenAny(tasks.Keys);
                tasks.Remove(delayTask);
                if (first != delayTask)
                    return await first;
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}