﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fclp.Internals.Extensions;
using log4net;

namespace ClusterClient.Clients
{
    internal class ParallelClusterClient : ClusterClientBase
    {
        public ParallelClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = new Dictionary<Task<string>, string>();
            using (Helper.AutoTaskAbort(tasks))
            {
                foreach (var uri in ReplicaAddresses)
                {
                    var queryString = $"{uri}?query={query}";
                    var task = GetResultTask(queryString);
                    tasks.Add(task, queryString);
                }

                var requestsTask = Task.WhenAny(tasks.Keys);
                if (await Task.WhenAny(requestsTask, Task.Delay(timeout)) == requestsTask)
                {
                    var res = await await requestsTask;
                    if (!res.IsNullOrEmpty())
                        return res;
                }
                throw new TimeoutException();
            }
        }
    }
}