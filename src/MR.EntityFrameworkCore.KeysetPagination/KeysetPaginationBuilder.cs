using System.Linq.Expressions;
using System.Reflection;
using MR.EntityFrameworkCore.KeysetPagination.Columns;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationBuilder<T>
	where T : class
{
	private readonly List<IKeysetColumn<T>> _columns = new();

	internal IReadOnlyList<IKeysetColumn<T>> Columns => _columns;

	public KeysetPaginationBuilder<T> Ascending(
		string propertyName,
		MethodInfo? jsonElementToValueConverter = null)
	{
		var memberExpression = ExpressionHelper.GetMemberExpressionFromString<T>(propertyName, jsonElementToValueConverter);
		return ConfigureColumn(memberExpression, isDescending: false);
	}

	public KeysetPaginationBuilder<T> Descending(
		string propertyName,
		MethodInfo? jsonElementToValueConverter = null)
	{
		var memberExpression = ExpressionHelper.GetMemberExpressionFromString<T>(propertyName, jsonElementToValueConverter);
		return ConfigureColumn(memberExpression, isDescending: true);
	}

	public KeysetPaginationBuilder<T> Ascending<TProp>(
		Expression<Func<T, TProp>> propertyExpression)
	{
		return ConfigureColumn(propertyExpression, isDescending: false);
	}

	public KeysetPaginationBuilder<T> Descending<TProp>(
		Expression<Func<T, TProp>> propertyExpression)
	{
		return ConfigureColumn(propertyExpression, isDescending: true);
	}

	private KeysetPaginationBuilder<T> ConfigureColumn(
		LambdaExpression propertyExpression,
		bool isDescending)
	{
		var unwrapped = ExpressionHelper.UnwrapConvertAndLambda(propertyExpression);
		if (ExpressionHelper.IsSimpleMemberAccess(unwrapped))
		{
			var property = ExpressionHelper.GetSimplePropertyFromMemberAccess(unwrapped);
			_columns.Add(new KeysetColumnSimple<T>(
				property,
				isDescending));
		}
		else if (ExpressionHelper.IsNestedMemberAccess(unwrapped))
		{
			var properties = ExpressionHelper.GetNestedPropertiesFromMemberAccess(unwrapped);
			_columns.Add(new KeysetColumnNested<T>(
				properties,
				isDescending));
		}
		else if (ExpressionHelper.IsJsonMemberAccess(unwrapped))
		{
			(var properties, var jsonProperties, var jsonElementToValue) = ExpressionHelper.GetJsonNestedPropertiesFromMemberAccess(unwrapped);
			_columns.Add(new KeysetColumnJsonProperty<T>(
				properties,
				jsonProperties,
				jsonElementToValue,
				isDescending
				));
		}
		else
		{
			throw new InvalidOperationException("Expression kind not found");
		}

		return this;
	}

}
