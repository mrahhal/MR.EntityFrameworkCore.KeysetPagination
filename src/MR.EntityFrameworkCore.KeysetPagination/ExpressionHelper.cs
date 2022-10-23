using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal static class ExpressionHelper
{
	public static Expression UnwrapConvert<T, TProp>(
		Expression<Func<T, TProp>> expression)
	{
		if (expression.Body.NodeType != ExpressionType.Convert)
		{
			return expression.Body;
		}

		return ((UnaryExpression)expression.Body).Operand;
	}

	public static bool IsSimpleMemberAccess(
		Expression expression)
	{
		ValidateExpressionUnwrapped(expression);

		return expression is MemberExpression memberExpression
			&& memberExpression.Expression is not MemberExpression;
	}

	public static PropertyInfo GetSimplePropertyFromMemberAccess(
		Expression expression)
	{
		ValidateExpressionUnwrapped(expression);

		var memberExpression = (MemberExpression)expression;
		return GetPropertyInfoMember(memberExpression);
	}

	public static List<PropertyInfo> GetNestedPropertiesFromMemberAccess(
		Expression expression)
	{
		ValidateExpressionUnwrapped(expression);

		var properties = new List<PropertyInfo>();

		var next = expression;
		while (next is MemberExpression memberExpression)
		{
			properties.Add(GetPropertyInfoMember(memberExpression));
			next = memberExpression.Expression;
		}

		properties.Reverse();
		return properties;
	}

	[Conditional("DEBUG")]
	private static void ValidateExpressionUnwrapped(Expression expression)
	{
		if (expression.NodeType is ExpressionType.Lambda or ExpressionType.Convert)
		{
			throw new Exception("Expression should have been unwrapped by now.");
		}
	}

	private static PropertyInfo GetPropertyInfoMember(MemberExpression memberExpression)
	{
		if (memberExpression.Member is PropertyInfo prop)
		{
			return prop;
		}

		throw new InvalidOperationException($"Expected a property access, got '{memberExpression.Member.MemberType}'.");
	}
}
