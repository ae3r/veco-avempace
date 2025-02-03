
namespace Application.Common.Mappings;

/// <summary>
/// MappingExtensions class
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// PaginatedListAsync
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize) where TDestination : class
        => PaginatedList<TDestination>.CreateAsync(queryable.AsNoTracking(), pageNumber, pageSize);

    /// <summary>
    /// PaginatedDataAsync
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static Task<PaginatedData<TDestination>> PaginatedDataAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize) where TDestination : class
            => PaginatedData<TDestination>.CreateAsync(queryable.AsNoTracking(), pageNumber, pageSize);

    /// <summary>
    /// ProjectToListAsync
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable, IConfigurationProvider configuration) where TDestination : class
                => queryable.ProjectTo<TDestination>(configuration).AsNoTracking().ToListAsync();
}
