using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal static class ExpressionHelper
{
	public static Expression UnwrapConvertAndLambda(
		LambdaExpression expression)
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
			&& memberExpression.Expression is not MemberExpression
			&& memberExpression.Expression.Type != typeof(JsonDocument);
	}

	public static bool IsNestedMemberAccess(Expression expression, Expression parent = null)
	{
		ValidateExpressionUnwrapped(expression);

		if (expression is MemberExpression memberExpression &&
			memberExpression.Expression != null &&
			memberExpression.Expression.NodeType == ExpressionType.Parameter &&
			parent != null)
		{
			return true;
		}

		if (expression is MemberExpression memberExpressionb &&
			memberExpressionb.Expression is MemberExpression &&
			memberExpressionb.Expression.Type != typeof(JsonElement))
		{
			return IsNestedMemberAccess(memberExpressionb.Expression, memberExpressionb);
		}

		return false;
	}

	public static bool IsJsonMemberAccess(Expression expression, Expression parent = null)
	{
		ValidateExpressionUnwrapped(expression);

		if (expression is MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Type == typeof(JsonElement) ||
				methodCallExpression.Object?.Type == typeof(JsonElement))
			{
				return true;
			}
		}

		return false;
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

	public static (List<PropertyInfo>, List<string>, MethodInfo?) GetJsonNestedPropertiesFromMemberAccess(
		Expression expression)
	{
		ValidateExpressionUnwrapped(expression);

		var properties = new List<PropertyInfo>();
		var jsonProperties = new List<string>();
		var jsonElementToValue = default(MethodInfo);

		var next = expression;
		// First get all the Json stuff from the expression
		while (next is MethodCallExpression methodCallExpression)
		{
			// This chained call represent all the JsonElements properties in sequence kind:
			// x.Inner.JsonData.RootElement.GetProperty("zz").GetProperty("yy").GetString()
			if ("GetProperty" == methodCallExpression.Method.Name)
			{
				var propertyName = methodCallExpression.Arguments.First() as ConstantExpression;
				if (propertyName != null)
				{
					jsonProperties.Add((string)propertyName.Value);
				}
			}
			// This call should represent the last call in sequence kind:
			// x.Inner.JsonData.RootElement.GetProperty("zz").GetProperty("yy").GetString()
			// This will be our converter to use for JsonColumns, can be null -> the default one will be used in the column
			else if (methodCallExpression.Method.Name.StartsWith("Get") &&
					!methodCallExpression.Arguments.Any())
			{
				jsonElementToValue = methodCallExpression.Method;
			}
			next = methodCallExpression.Object;
		}
		jsonProperties.Reverse();

		// Then Get all the POCO props from the expression
		while (next is MemberExpression memberExpression)
		{
			properties.Add(GetPropertyInfoMember(memberExpression));
			next = memberExpression.Expression;
		}
		properties.Reverse();

		return (properties, jsonProperties, jsonElementToValue);
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

	private static string[] GetProperties(string stringProperty) => stringProperty.Trim().Split('.');

	public static LambdaExpression GetMemberExpressionFromString<TEntity>(
		string propertyName,
		MethodInfo? jsonElementToValueConverter = null)
	{
		var props = GetProperties(propertyName);

		var param = Expression.Parameter(typeof(TEntity), "entity");
		if (props.Length > 1)
		{
			var nestedExpr = default(MemberExpression);
			for (var i = 0; i < props.Length; i++)
			{
				var chainAnchor = nestedExpr ?? (Expression)param;
				nestedExpr = Expression.Property(chainAnchor, props[i]);
				if (nestedExpr.Type == typeof(JsonDocument))
				{
					var jsonExpr = AppendJsonElementsExpression(nestedExpr, props.Skip(i + 1).ToList());
					if (jsonElementToValueConverter != null)
					{
						jsonExpr = Expression.Call(jsonExpr, jsonElementToValueConverter);
					}
					return Expression.Lambda(jsonExpr, param);
				}
			}

			return Expression.Lambda(nestedExpr!, param);
		}

		var simpleExpr = Expression.Property(param, propertyName);
		return Expression.Lambda(simpleExpr, param);
	}

	public static Expression AppendJsonElementsExpression(MemberExpression resultingExpression, List<string> props)
	{
		if (!props.Any())
		{
			throw new ArgumentException("Json properties list is empty and must contains at least one JsonElement to access.");
		}

		// Chain the root element of a JsonDocument (System:Text:JsonDocument type)
		var documentElementProperty = Expression.Property(resultingExpression, "RootElement");

		var getPropMeth = documentElementProperty.Type.GetMethod(nameof(JsonElement.GetProperty),
							bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
							null,
							new[] { typeof(string) }, null);
		if (getPropMeth == null)
		{
			throw new InvalidOperationException("Connot find 'GetProperty(string)' on 'JsonElement'.");
		}

		// Iterate over Json props and chain a call to GetProperty method on it
		var propertiesGettersExpression = default(MethodCallExpression);
		foreach (var prop in props)
		{
			var chainAnchor = propertiesGettersExpression ?? (Expression)documentElementProperty;
			propertiesGettersExpression = Expression.Call(chainAnchor, getPropMeth,
															Expression.Constant(prop));
		}

		return propertiesGettersExpression!;
	}
}
