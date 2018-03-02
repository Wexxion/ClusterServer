using System.Collections.Generic;
using System.Linq;

namespace ClusterClient
{
    public class ServerStatistics
    {
        private readonly Dictionary<string, Stat> statistics;

        public ServerStatistics(string[] replicaAddresses)
        {
            statistics = new Dictionary<string, Stat>();
            foreach (var replicaAddress in replicaAddresses)
                statistics.Add(replicaAddress, new Stat());
        }
        public string[] GetSorterAddresses()
        {
            lock (statistics)
                return statistics.OrderBy(pair => pair.Value.Average).Select(pair => pair.Key).ToArray();
        }

        public void AddStatistics(string replicaAddress, long time)
        {
            lock (statistics)
            {
                var replicaStat = statistics[replicaAddress];
                replicaStat.Count++;
                if (replicaStat.Count == 1)
                    replicaStat.Average = time;
                else
                    replicaStat.Average = (time + (replicaStat.Count - 1) * replicaStat.Average) / replicaStat.Count;
            }
        }
        private class Stat
        {
            public long Count { get; set; }
            public long Average { get; set; }
        }
    }
}