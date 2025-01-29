
namespace Application.Common.Specification;

/// <summary>
/// Specification abstract class
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Specification<T> : ISpecification<T> where T : class, IEntity
{
    public Expression<Func<T, bool>> Criteria { get; set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// AddInclude when param is object
    /// </summary>
    /// <param name="includeExpression"></param>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// AddInclude when param is string
    /// </summary>
    /// <param name="includeString"></param>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// And
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public Expression<Func<T, bool>> And(Expression<Func<T, bool>> query)
    {
        return Criteria = Criteria == null ? query : Criteria.And(query);
    }

    /// <summary>
    /// Or
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> query)
    {
        return Criteria = Criteria == null ? query : Criteria.Or(query);
    }
}
