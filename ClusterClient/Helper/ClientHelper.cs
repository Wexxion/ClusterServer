using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClusterClient.Helper
{
    public class ClientHelper
    {
        public ServerStatistics Statistics { get; }
        public GrayList GrayList { get; }
        public int ServerСount { get; }

        private readonly IEnumerator<string> enumerator;
        public ClientHelper(string[] replicaAddresses)
        {
            Statistics = new ServerStatistics(replicaAddresses);
            GrayList = new GrayList(replicaAddresses);
            
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
                if (GrayList.IsReady(currAddr))
                    yield return currAddr;
                pointer = (pointer + 1) % currStat.Length;
            }
        }

        public void RemoveAddress(string address)
        {
            Statistics.RemoveAddress(address);
            GrayList.Remove(address);
        }
    }
}