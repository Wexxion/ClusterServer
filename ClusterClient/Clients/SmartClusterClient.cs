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
        private Random rnd;
        private List<Task<string>> tasks;
        private AutoResetEvent autoResetEvent;
        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            rnd = new Random();
            autoResetEvent = new AutoResetEvent(false);
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {   
            tasks = new List<Task<string>>();

            var orderArray = Enumerable.Range(0, ReplicaAddresses.Length).OrderBy(x => rnd.Next()).ToArray();
            var newTimeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / ReplicaAddresses.Length);
            
            StartRound(orderArray, 0, query, newTimeout);
            return await await Task.Run(() =>
            {
                autoResetEvent.WaitOne();
                return Task.WhenAny(tasks);
            });
            //autoResetEvent.WaitOne();
            //return await await Task.WhenAny(tasks);
        }

        private void StartRound(int[] orderArray, int i, string query, TimeSpan timeout)
        {
            Task.Run(async () =>
            {
                var webRequest = CreateRequest(ReplicaAddresses[orderArray[i]] + "?query=" + query);
                Log.InfoFormat("Processing {0}", webRequest.RequestUri);
                var task = ProcessRequestAsync(webRequest);
                tasks.Add(task);
                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    autoResetEvent.Set();
                else
                    StartRound(orderArray, (i + 1) % orderArray.Length, query, timeout);
            });
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}