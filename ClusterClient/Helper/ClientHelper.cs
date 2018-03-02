using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClusterClient.Helper;

namespace ClusterClient
{
    public class ClientHelper
    {
        private readonly ServerStatistics statistics;
        private readonly Dictionary<string, Task> grayList;
        private readonly IEnumerator<string> enumerator;
        public int ServerСount { get; }
        public ClientHelper(string[] replicaAddresses)
        {
            statistics = new ServerStatistics(replicaAddresses);
            grayList = new Dictionary<string, Task>();
            foreach (var address in replicaAddresses)
                grayList.Add(address, Task.Delay(0));
            ServerСount = replicaAddresses.Length;
            enumerator = GetAddresses();
            enumerator.MoveNext();
        }

        public AutoTaskAbort AutoTaskAbort(Dictionary<Task<string>, string> tasks) => new AutoTaskAbort(tasks);
        public string GetNextAddress()
        {
            var res =  enumerator.Current;
            enumerator.MoveNext();
            return res;
        }
        private IEnumerator<string> GetAddresses()
        {
            var pointer = 0;
            var currStat = statistics.GetSorterAddresses();
            while (true)
            {
                if (pointer == 0)
                    currStat = statistics.GetSorterAddresses();
                var currAddr = currStat[pointer++];
                if (grayList[currAddr].IsCompleted)
                    yield return currAddr;
                pointer %= ServerСount;
            }
        }

        public void AddStatistics(string adress, long time) => statistics.AddStatistics(adress, time);
        public void AddToGrayList(string adress, TimeSpan timeout) => grayList[adress] = Task.Delay(timeout);
    }
}