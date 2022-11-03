using System.Linq.Expressions;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationBuilder<T>
	where T : class
{
	private readonly List<KeysetColumn<T>> _columns = new();

	internal IReadOnlyList<KeysetColumn<T>> Columns => _columns;

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

	private KeysetPaginationBuilder<T> ConfigureColumn<TProp>(
		Expression<Func<T, TProp>> propertyExpression,
		bool isDescending)
	{
		var unwrapped = ExpressionHelper.UnwrapConvertAndLambda(propertyExpression);
		if (ExpressionHelper.IsSimpleMemberAccess(unwrapped))
		{
			var property = ExpressionHelper.GetSimplePropertyFromMemberAccess(unwrapped);
			_columns.Add(new KeysetColumnSimple<T, TProp>(
				property,
				isDescending));
		}
		else
		{
			var properties = ExpressionHelper.GetNestedPropertiesFromMemberAccess(unwrapped);
			_columns.Add(new KeysetColumnNested<T, TProp>(
				properties,
				isDescending));
		}

		return this;
	}
}
