using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Application.Common.Extensions;

/// <summary>
/// PredicateBuilder class
/// </summary>
public static class PredicateBuilder
{
    #region -- Public methods --

    /// <summary>
    /// FromFilter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="filters"></param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> FromFilter<T>(string filters)
    {
        Expression<Func<T, bool>> any = x => true;
        if (!string.IsNullOrEmpty(filters))
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            opts.Converters.Add(new AutoNumberToStringConverter());
            var filterRules = JsonSerializer.Deserialize<FilterRule[]>(filters, opts);

            foreach (var filter in filterRules)
            {
                if (Enum.TryParse(filter.op, out OperationExpression op) && !string.IsNullOrEmpty(filter.value))
                {
                    var expression = GetCriteriaWhere<T>(filter.field, op, filter.value);
                    any = any.And(expression);
                }
            }
        }

        return any;
    }

    /// <summary>
    /// And
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression p = left.Parameters.First();
        SubstExpressionVisitor visitor = new SubstExpressionVisitor
        {
            Subst = { [right.Parameters.First()] = p }
        };

        Expression body = Expression.AndAlso(left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    /// <summary>
    /// Or
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {

        ParameterExpression p = left.Parameters.First();
        SubstExpressionVisitor visitor = new SubstExpressionVisitor
        {
            Subst = { [right.Parameters.First()] = p }
        };

        Expression body = Expression.OrElse(left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    #endregion

    #region -- Private methods --

    /// <summary>
    /// GetCriteriaWhere
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fieldName"></param>
    /// <param name="selectedOperator"></param>
    /// <param name="fieldValue"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static Expression<Func<T, bool>> GetCriteriaWhere<T>(string fieldName, OperationExpression selectedOperator, object fieldValue)
    {
        var props = TypeDescriptor.GetProperties(typeof(T));
        var prop = GetProperty(props, fieldName, true);
        var parameter = Expression.Parameter(typeof(T));
        var expressionParameter = GetMemberExpression<T>(parameter, fieldName);
        if (prop != null && fieldValue != null)
        {
            BinaryExpression body = null;
            if (prop.PropertyType.IsEnum)
            {
                if (Enum.IsDefined(prop.PropertyType, fieldValue))
                {
                    object value = Enum.Parse(prop.PropertyType, fieldValue.ToString(), true);
                    body = Expression.Equal(expressionParameter, Expression.Constant(value));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                }
                else
                {
                    return x => false;
                }
            }
            switch (selectedOperator)
            {
                case OperationExpression.equal:
                    body = Expression.Equal(expressionParameter, Expression.Constant(Convert.ChangeType(fieldValue.ToString() == "null" ? null : fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                case OperationExpression.notequal:
                    body = Expression.NotEqual(expressionParameter, Expression.Constant(Convert.ChangeType(fieldValue.ToString() == "null" ? null : fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                case OperationExpression.less:
                    body = Expression.LessThan(expressionParameter, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                case OperationExpression.lessorequal:
                    body = Expression.LessThanOrEqual(expressionParameter, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                case OperationExpression.greater:
                    body = Expression.GreaterThan(expressionParameter, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                case OperationExpression.greaterorequal:
                    body = Expression.GreaterThanOrEqual(expressionParameter, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(body, parameter);
                case OperationExpression.contains:
                    var contains = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var bodyLike = Expression.Call(expressionParameter, contains, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(bodyLike, parameter);
                case OperationExpression.endwith:
                    var endswith = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
                    var bodyendwith = Expression.Call(expressionParameter, endswith, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(bodyendwith, parameter);
                case OperationExpression.beginwith:
                    var startswith = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
                    var bodystartswith = Expression.Call(expressionParameter, startswith, Expression.Constant(Convert.ChangeType(fieldValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType), prop.PropertyType));
                    return Expression.Lambda<Func<T, bool>>(bodystartswith, parameter);
                case OperationExpression.includes:
                    return Includes<T>(fieldValue, parameter, expressionParameter, prop.PropertyType);
                case OperationExpression.between:
                    return Between<T>(fieldValue, parameter, expressionParameter, prop.PropertyType);
                default:
                    throw new ArgumentException("OperationExpression");
            }
        }
        else
        {
            return x => false;
        }
    }

    /// <summary>
    /// GetMemberExpression
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parameter"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    private static MemberExpression GetMemberExpression<T>(ParameterExpression parameter, string propName)
    {
        if (string.IsNullOrEmpty(propName))
        {
            return null;
        }

        var propertiesName = propName.Split('.');
        if (propertiesName.Length == 2)
        {
            return Expression.Property(Expression.Property(parameter, propertiesName[0]), propertiesName[1]);
        }

        return Expression.Property(parameter, propName);
    }

    /// <summary>
    /// Includes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fieldValue"></param>
    /// <param name="parameterExpression"></param>
    /// <param name="memberExpression"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private static Expression<Func<T, bool>> Includes<T>(object fieldValue, ParameterExpression parameterExpression, MemberExpression memberExpression, Type type)
    {
        var safetype = Nullable.GetUnderlyingType(type) ?? type;

        switch (safetype.Name.ToLower())
        {
            case "string":
                var strlist = fieldValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                if (strlist == null || strlist.Count == 0)
                {
                    return x => true;
                }
                var strmethod = typeof(List<string>).GetMethod("Contains", new Type[] { typeof(string) });
                var strcallexp = Expression.Call(Expression.Constant(strlist.ToList()), strmethod, memberExpression);
                return Expression.Lambda<Func<T, bool>>(strcallexp, parameterExpression);
            case "int32":
                var intlist = fieldValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                if (intlist == null || intlist.Count == 0)
                {
                    return x => true;
                }
                var intmethod = typeof(List<int>).GetMethod("Contains", new Type[] { typeof(int) });
                var intcallexp = Expression.Call(Expression.Constant(intlist.ToList()), intmethod, memberExpression);
                return Expression.Lambda<Func<T, bool>>(intcallexp, parameterExpression);
            case "float":
            case "decimal":
                var floatlist = fieldValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Decimal.Parse).ToList();
                if (floatlist == null || floatlist.Count == 0)
                {
                    return x => true;
                }
                var floatmethod = typeof(List<decimal>).GetMethod("Contains", new Type[] { typeof(decimal) });
                var floatcallexp = Expression.Call(Expression.Constant(floatlist.ToList()), floatmethod, memberExpression);
                return Expression.Lambda<Func<T, bool>>(floatcallexp, parameterExpression);
            default:
                return x => true;
        }

    }

    /// <summary>
    /// Between
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fieldValue"></param>
    /// <param name="parameterExpression"></param>
    /// <param name="memberExpression"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private static Expression<Func<T, bool>> Between<T>(object fieldValue, ParameterExpression parameterExpression, MemberExpression memberExpression, Type type)
    {

        var safetype = Nullable.GetUnderlyingType(type) ?? type;
        switch (safetype.Name.ToLower())
        {
            case "datetime":
                var datearray = ((string)fieldValue).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                var start = Convert.ToDateTime(datearray[0] + " 00:00:00", CultureInfo.CurrentCulture);
                var end = Convert.ToDateTime(datearray[1] + " 23:59:59", CultureInfo.CurrentCulture);
                var greater = Expression.GreaterThanOrEqual(memberExpression, Expression.Constant(start, type));
                var less = Expression.LessThanOrEqual(memberExpression, Expression.Constant(end, type));
                return Expression.Lambda<Func<T, bool>>(greater, parameterExpression)
                  .And(Expression.Lambda<Func<T, bool>>(less, parameterExpression));
            case "int":
            case "int32":
                var intarray = ((string)fieldValue).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                var min = Convert.ToInt32(intarray[0], CultureInfo.CurrentCulture);
                var max = Convert.ToInt32(intarray[1], CultureInfo.CurrentCulture);
                var maxthen = Expression.GreaterThanOrEqual(memberExpression, Expression.Constant(min, type));
                var minthen = Expression.LessThanOrEqual(memberExpression, Expression.Constant(max, type));
                return Expression.Lambda<Func<T, bool>>(maxthen, parameterExpression)
                  .And(Expression.Lambda<Func<T, bool>>(minthen, parameterExpression));
            case "decimal":
                var decarray = ((string)fieldValue).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                var dmin = Convert.ToDecimal(decarray[0], CultureInfo.CurrentCulture);
                var dmax = Convert.ToDecimal(decarray[1], CultureInfo.CurrentCulture);
                var dmaxthen = Expression.GreaterThanOrEqual(memberExpression, Expression.Constant(dmin, type));
                var dminthen = Expression.LessThanOrEqual(memberExpression, Expression.Constant(dmax, type));
                return Expression.Lambda<Func<T, bool>>(dmaxthen, parameterExpression)
                  .And(Expression.Lambda<Func<T, bool>>(dminthen, parameterExpression));
            case "float":
                var farray = ((string)fieldValue).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                var fmin = Convert.ToDecimal(farray[0], CultureInfo.CurrentCulture);
                var fmax = Convert.ToDecimal(farray[1], CultureInfo.CurrentCulture);
                var fmaxthen = Expression.GreaterThanOrEqual(memberExpression, Expression.Constant(fmin, type));
                var fminthen = Expression.LessThanOrEqual(memberExpression, Expression.Constant(fmax, type));
                return Expression.Lambda<Func<T, bool>>(fmaxthen, parameterExpression)
                  .And(Expression.Lambda<Func<T, bool>>(fminthen, parameterExpression));
            case "string":
                var strarray = ((string)fieldValue).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                var strstart = strarray[0];
                var strend = strarray[1];
                var strmethod = typeof(string).GetMethod("CompareTo", new[] { typeof(string) });
                var callcomparetostart = Expression.Call(memberExpression, strmethod, Expression.Constant(strstart, type));
                var callcomparetoend = Expression.Call(memberExpression, strmethod, Expression.Constant(strend, type));
                var strgreater = Expression.GreaterThanOrEqual(callcomparetostart, Expression.Constant(0));
                var strless = Expression.LessThanOrEqual(callcomparetoend, Expression.Constant(0));
                return Expression.Lambda<Func<T, bool>>(strgreater, parameterExpression)
                  .And(Expression.Lambda<Func<T, bool>>(strless, parameterExpression));
            default:
                return x => true;
        }

    }

    /// <summary>
    /// GetProperty
    /// </summary>
    /// <param name="props"></param>
    /// <param name="fieldName"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    private static PropertyDescriptor GetProperty(PropertyDescriptorCollection props, string fieldName, bool ignoreCase)
    {
        if (!fieldName.Contains('.'))
        {
            return props.Find(fieldName, ignoreCase);
        }

        var fieldNameProperty = fieldName.Split('.');
        return props.Find(fieldNameProperty[0], ignoreCase).GetChildProperties().Find(fieldNameProperty[1], ignoreCase);

    }

    #endregion

}
