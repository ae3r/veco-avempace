using Application.Features.Produits.Caching;
using Application.Features.Produits.DTOs;

namespace Application.Features.Produits.Queries.PaginationQuery;

/// <summary>
///ProduitsWithPaginationQuery class
/// </summary>
public class ProduitsWithPaginationQuery : PaginationRequest, IRequest<PaginatedData<ProduitDto>>
{
    public string CacheKey => ProduitCacheKey.GetPaginationCacheKey(this.ToString());

    public MemoryCacheEntryOptions Options => ProduitCacheKey.MemoryCacheEntryOptions;

}
