using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ClusterClient.Helper
{
    public class GrayList
    {
        private readonly ConcurrentDictionary<string, Task> grayList;

        public GrayList(string[] replicaAddresses)
        {
            grayList = new ConcurrentDictionary<string, Task>();
            foreach (var address in replicaAddresses)
                grayList.TryAdd(address, Task.Delay(0));
        }

        public bool IsReady(string replicaAddress) => grayList[replicaAddress].IsCompleted;
        public void Add(string replicaAddress, TimeSpan banTime) => grayList[replicaAddress] = Task.Delay(banTime);
        public void Remove(string replicaAddress) => grayList.TryRemove(replicaAddress, out _);
    }
}