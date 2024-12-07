
namespace Application.Common.Interfaces.Caching;

/// <summary>
/// ICacheInvalidator interface
/// </summary>
public interface ICacheInvalidator
{
    string CacheKey { get; }
    CancellationTokenSource ResetCacheToken { get; }
}
