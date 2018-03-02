using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClusterClient.Clients;

namespace ClusterClient.Helper
{
    public class AutoTaskAbort : IDisposable
    {
        private readonly Dictionary<Task<string>, string> tasks;
        public AutoTaskAbort( Dictionary<Task<string>, string> tasks) => this.tasks = tasks;

        public void Dispose()
        {
            foreach (var pair in tasks)
                if (!pair.Key.IsCompleted && pair.Value != null)
                    Task.Run(async () => 
                        await ClusterClientBase.ProcessRequestAsync(ClusterClientBase.CreateRequest(pair.Value, true)));
        }
    }
}