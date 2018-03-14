using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClusterClient.Helper
{
    public class ClientHelper
    {
        public ServerStatistics Statistics { get; }
        private readonly Dictionary<string, Task> grayList;
        private readonly IEnumerator<string> enumerator;
        public int ServerСount { get; }
        public ClientHelper(string[] replicaAddresses)
        {
            Statistics = new ServerStatistics(replicaAddresses);
            grayList = new Dictionary<string, Task>();
            foreach (var address in replicaAddresses)
                grayList.Add(address, Task.Delay(0));
            ServerСount = replicaAddresses.Length;
            enumerator = GetAddressesEnumerator();
            enumerator.MoveNext();
        }

        public AutoTaskAbort AutoTaskAbort(Dictionary<Task<string>, string> tasks) => new AutoTaskAbort(tasks);
        public string GetNextAddress()
        {
            var res =  enumerator.Current;
            enumerator.MoveNext();
            return res;
        }
        private IEnumerator<string> GetAddressesEnumerator()
        {
            var pointer = 0;
            var currStat = Statistics.GetSortedAddresses();
            while (true)
            {
                if (pointer == 0)
                    currStat = Statistics.GetSortedAddresses();
                var currAddr = currStat[pointer];
                if (grayList[currAddr].IsCompleted)
                    yield return currAddr;
                pointer = (pointer + 1) % currStat.Length;
            }
        }
        public void AddToGrayList(string address, TimeSpan timeout) => grayList[address] = Task.Delay(timeout);
    }
}