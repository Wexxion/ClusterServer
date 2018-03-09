using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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
                if (!pair.Key.IsCompleted)
                    Task.Run(async () => await ProcessRequestAsync(ClusterClientBase.CreateRequest(pair.Value, true)));
            tasks.Clear();
        }

        private static async Task<string> ProcessRequestAsync(WebRequest request)
        {
            using (var response = await request.GetResponseAsync())
                return await new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEndAsync();
        }
    }
}