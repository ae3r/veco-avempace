
namespace Application.Features.Clients.Caching;

/// <summary>
/// ClientCacheKey class
/// </summary>
public static class ClientCacheKey
{
    public const string GetAllCacheKey = Constants.Constants.AllCacheKeyClients;
    public static string GetPaginationCacheKey(string parameters)
    {
        return $"ClientsWithPaginationQuery,{parameters}";
    }
    static ClientCacheKey()
    {
        _tokensource = new CancellationTokenSource(new TimeSpan(1, 0, 0));
    }
    private static CancellationTokenSource _tokensource;
    public static CancellationTokenSource SharedExpiryTokenSource()
    {
        if (_tokensource.IsCancellationRequested)
        {
            _tokensource = new CancellationTokenSource(new TimeSpan(3, 0, 0));
        }
        return _tokensource;
    }
    public static MemoryCacheEntryOptions MemoryCacheEntryOptions => new MemoryCacheEntryOptions().AddExpirationToken(new CancellationChangeToken(SharedExpiryTokenSource().Token));
}
