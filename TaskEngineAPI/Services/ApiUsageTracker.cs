using System.Collections.Concurrent;

namespace TaskEngineAPI.Services
{
    public class ApiUsageTracker
    {
        public class UsageData
        {
            public int TotalRequests = 0;
            public HashSet<string> UniqueUsers = new HashSet<string>();
        }


        private static readonly ConcurrentDictionary<string, UsageData> _apiUsage = new();

        public static void Track(string endpoint, string userCode)
        {
            var usage = _apiUsage.GetOrAdd(endpoint, _ => new UsageData());

            Interlocked.Increment(ref usage.TotalRequests);
            lock (usage.UniqueUsers)
            {
                usage.UniqueUsers.Add(userCode);
            }
        }

        public static Dictionary<string, object> GetUsageSummary()
        {
            return _apiUsage.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    TotalRequests = kvp.Value.TotalRequests,
                    UniqueUsers = kvp.Value.UniqueUsers.Count,
                    Users = kvp.Value.UniqueUsers.ToList()
                } as object
            );
        }

    }
}
