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
            var tasks = new HashSet<Task<string>>();

            var orderArray = Enumerable.Range(0, ReplicaAddresses.Length).OrderBy(x => rnd.Next()).ToArray();
            var newTimeout = TimeSpan.FromMilliseconds(totaltimeout.TotalMilliseconds / ReplicaAddresses.Length);

            var roundTask = StartRound(tasks, orderArray, query, newTimeout);
            if (await Task.WhenAny(roundTask, Task.Delay(totaltimeout)) == roundTask)
                return await roundTask;
            throw new TimeoutException();
        }

        private async Task<string> GetDelayStringTask(TimeSpan delay)
        {
            await Task.Delay(delay);
            return null;
        }

        private async Task<string> StartRound(HashSet<Task<string>> tasks, int[] orderArray, string query, TimeSpan newTimeout)
        {
            return await Task.Run(async () =>
            {
                var i = 0;
                while (true)
                {
                    var webRequest =
                        CreateRequest(ReplicaAddresses[orderArray[i++ % orderArray.Length]] + "?query=" + query);
                    Log.InfoFormat("Processing {0}", webRequest.RequestUri);

                    var task = ProcessRequestAsync(webRequest);
                    var delayTask = GetDelayStringTask(newTimeout);

                    tasks.Add(task);
                    tasks.Add(delayTask);

                    var first = await Task.WhenAny(tasks);
                    if (first == delayTask)
                        tasks.Remove(delayTask);
                    else
                        return await first;
                }
            });
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}