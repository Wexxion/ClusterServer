﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fclp.Internals.Extensions;
using log4net;

namespace ClusterClient.Clients
{
    public class SmartClusterClient : ClusterClientBase
    {
        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses) {}

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = new Dictionary<Task<string>, string>();
            var newTimeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / ReplicaAddresses.Length);
            var roundTask = Task.Run(async () =>
            {
                while (true)
                {
                    var address = Helper.GetNextAddress();
                    var queryString = $"{address}?query={query}";
                    var task = GetResultTask(queryString);
                    var delayTask = Task.Delay(newTimeout);

                    tasks.Add(task, queryString);
                    var firstRequestTask = Task.WhenAny(tasks.Keys);
                    var first = await Task.WhenAny(firstRequestTask, delayTask);

                    if (first != delayTask)
                    {
                        if (first != task)
                            Helper.GrayList.Add(address, newTimeout);
                        using (Helper.AutoTaskAbort(tasks))
                            return await await firstRequestTask;
                    }
                }
            });
            if (await Task.WhenAny(roundTask, Task.Delay(timeout)) == roundTask)
            {
                var res = await roundTask;
                if (!res.IsNullOrEmpty())
                    return res;
            }
            throw new TimeoutException();
        }
    }
}