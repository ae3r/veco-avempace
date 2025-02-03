using Domain.Common;
using System.Linq.Expressions;

namespace Application.Common.Interfaces;

/// <summary>
/// ISpecification interface
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISpecification<T> where T : class, IEntity
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, bool>> And(Expression<Func<T, bool>> query);
    Expression<Func<T, bool>> Or(Expression<Func<T, bool>> query);
}
