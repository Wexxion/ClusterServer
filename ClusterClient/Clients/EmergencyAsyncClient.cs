using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fclp.Internals.Extensions;
using log4net;

namespace ClusterClient.Clients
{
    public class EmergencyAsyncClient : ClusterClientBase
    {
        private readonly string fastestReplicaAddress;

        public EmergencyAsyncClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            var task = Task.Run(GetFastestAsyncReplica);
            task.Wait();
            fastestReplicaAddress = task.Result;
        }

        public EmergencyAsyncClient(string[] replicaAddresses, string fastestReplica) : base(replicaAddresses)
        {
            fastestReplicaAddress = fastestReplica;
        }

        private async Task<string> GetFastestAsyncReplica()
        {
            var tasks = new Dictionary<string, Task<long[]>>();
            foreach (var uri in ReplicaAddresses)
            {
                var task = Task.Run(async () =>
                {
                    var res = new long[2];
                    var sw = Stopwatch.StartNew();
                    var testTasks = new[] {GetResultTask($"{uri}?query=test1"), GetResultTask($"{uri}?query=test2")};
                    await Task.WhenAny(testTasks);
                    res[0] = sw.ElapsedMilliseconds;
                    await Task.WhenAll(testTasks);
                    res[1] = sw.ElapsedMilliseconds;
                    return res;
                });
                tasks.Add(uri, task);
            }

            await Task.WhenAll(tasks.Values);

            string fastestReplica = null;
            var minTime = long.MaxValue;

            foreach (var address in tasks.Keys)
            {
                var timeResult = await tasks[address];
                var isAsync = Math.Abs(timeResult[0] - timeResult[1]) < timeResult[1] / 10;
                if (!isAsync) continue;
                var currTime = timeResult.Min();
                if (currTime < minTime)
                {
                    minTime = currTime;
                    fastestReplica = address;
                }
            }
            return fastestReplica;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(EmergencyAsyncClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            if (fastestReplicaAddress == null) throw new TimeoutException();
            var queryString = $"{fastestReplicaAddress}?query={query}";
            var requestsTask = GetResultTask(queryString);
            if (await Task.WhenAny(requestsTask, Task.Delay(timeout)) == requestsTask)
            {
                var res = await requestsTask;
                if (!res.IsNullOrEmpty())
                    return res;
            }
            throw new TimeoutException();
        }
    }
}