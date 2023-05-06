using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public record PurchaseStatus(string Status, DateTimeOffset Timestamp);

public static class PurchaseStatusHelper
{
    private static readonly IMemoryCache _cache;
    private static readonly ILogger<PurchaseStatus> _logger;

    static PurchaseStatusHelper()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PurchaseStatus>();
    }

    public static void SetPurchaseStatus(string status)
    {
        var statusList = GetPurchaseStatusList();

        // Add the current status to the list
        statusList.Add(new PurchaseStatus(status, DateTimeOffset.UtcNow));

        // Update the status list in the cache
        _cache.Set("PurchaseStatus", statusList);

        _logger.LogInformation($"Purchase status updated: CurrentStatus={status}, PreviousStatus={statusList[0].Status}");
    }

    public static List<PurchaseStatus> GetPurchaseStatusList()
    {
        var statusList = _cache.Get<List<PurchaseStatus>>("PurchaseStatus");

        if (statusList != null && statusList.Count > 0)
            _logger.LogInformation($"Purchase status list retrieved: Count={statusList.Count}");
        else
        {
            _logger.LogWarning($"No purchase status list found");
            statusList = new List<PurchaseStatus>();
        }

        return statusList;
    }

    public static List<PurchaseStatus> Clear()
    {
        var statusList = new List<PurchaseStatus>();
        _cache.Set("PurchaseStatus", statusList);

        return statusList;
    }
}
