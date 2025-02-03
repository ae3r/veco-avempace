using System.Linq.Expressions;

namespace Application.Common.Extensions;

/// <summary>
/// SubstExpressionVisitor class
/// </summary>
internal class SubstExpressionVisitor : ExpressionVisitor
{
    public Dictionary<Expression, Expression> Subst = new();

    /// <summary>
    /// VisitParameter 
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (Subst.TryGetValue(node, out var newValue))
        {
            return newValue;
        }
        return node;
    }
}
