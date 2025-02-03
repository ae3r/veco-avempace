
namespace Application.Features.Produits.Caching;

/// <summary>
/// ProduitCacheKey class
/// </summary>
public static class ProduitCacheKey
{
    public const string GetAllCacheKey = Constants.Constants.AllCacheKeyProduits;
    public static string GetPaginationCacheKey(string parameters)
    {
        return $"ProduitsWithPaginationQuery,{parameters}";
    }
    static ProduitCacheKey()
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
