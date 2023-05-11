using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal static class ExpressionHelper
{
	public static Expression UnwrapConvertAndLambda<T, TProp>(
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

	/// <summary>
	/// Gets the first expression from a <see cref="MemberExpression"/>. This is the `x` in `x.Prop1.Prop2`.
	/// </summary>
	public static Expression GetStartingExpression(
		MemberExpression expression)
	{
		ValidateExpressionUnwrapped(expression);

		var current = (Expression)expression;
		while (current is MemberExpression memberExpression)
		{
			current = memberExpression.Expression;
		}

		return current!;
	}

	public static PropertyInfo GetSimpleProperty(
		MemberExpression expression)
	{
		ValidateExpressionUnwrapped(expression);

		return GetPropertyInfoMember(expression);
	}

	public static List<PropertyInfo> GetNestedProperties(
		MemberExpression expression)
	{
		ValidateExpressionUnwrapped(expression);

		var properties = new List<PropertyInfo>();

		var current = (Expression)expression;
		while (current is MemberExpression memberExpression)
		{
			properties.Add(GetPropertyInfoMember(memberExpression));
			current = memberExpression.Expression;
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
