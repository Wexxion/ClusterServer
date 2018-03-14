using System.Collections.Concurrent;
using System.Linq;

namespace ClusterClient.Helper
{
    public class ServerStatistics
    {
        private readonly ConcurrentDictionary<string, Stat> statistics;

        public ServerStatistics(string[] replicaAddresses)
        {
            statistics = new ConcurrentDictionary<string, Stat>();
            foreach (var replicaAddress in replicaAddresses)
                statistics.TryAdd(replicaAddress, new Stat());
        }
        public string[] GetSortedAddresses()
        {
            return statistics.OrderBy(pair => pair.Value.Average).Select(pair => pair.Key).ToArray();
        }

        public void RemoveAddress(string replicaAddress) => statistics.TryRemove(replicaAddress, out _);

        public void AddStatistics(string replicaAddress, long time)
        {
                var replicaStat = statistics[replicaAddress];
                replicaStat.Count++;
                if (replicaStat.Count == 1)
                    replicaStat.Average = time;
                else
                    replicaStat.Average = (time + (replicaStat.Count - 1) * replicaStat.Average) / replicaStat.Count;
        }
        private class Stat
        {
            public long Count { get; set; }
            public long Average { get; set; }
        }
    }
}