using ASC.Web.Data;
using ASC.Web.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

public class NavigationCacheOperations : INavigationCacheOperations
{
    private readonly IDistributedCache _cache;
    private readonly string NavigationCacheName = "NavigationCache";

    public NavigationCacheOperations(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task CreateNavigationCacheAsync()
    {
        try
        {
            await _cache.SetStringAsync(NavigationCacheName, File.ReadAllText("Navigation/Navigation.json"));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Không thể đọc tệp Navigation.json", ex);
        }
    }

    public async Task<NavigationMenu> GetNavigationCacheAsync()
    {
        string? cachedValue = await _cache.GetStringAsync(NavigationCacheName);
        if (string.IsNullOrEmpty(cachedValue))
        {
            throw new InvalidOperationException("Dữ liệu cache điều hướng không tồn tại.");
        }

        var result = JsonConvert.DeserializeObject<NavigationMenu>(cachedValue);
        if (result == null)
        {
            throw new JsonException("Không thể giải mã dữ liệu điều hướng từ cache.");
        }
        return result;
    }
}