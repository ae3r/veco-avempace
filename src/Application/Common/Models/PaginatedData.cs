
namespace Application.Common.Models;

/// <summary>
/// PaginatedData class
/// </summary>
/// <typeparam name="T"></typeparam>
public class PaginatedData<T>
{
    public int total { get; set; }
    public IEnumerable<T> rows { get; set; }
    /// <summary>
    /// Constructor : Initializes a new instance of PaginatedData
    /// </summary>
    /// <param name="items"></param>
    /// <param name="total"></param>
    public PaginatedData(IEnumerable<T> items, int total)
    {
        this.rows = items;
        this.total = total;
    }

    /// <summary>
    /// CreateAsync
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static async Task<PaginatedData<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedData<T>(items, count);
    }
}
