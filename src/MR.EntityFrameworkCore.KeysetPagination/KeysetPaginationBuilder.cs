using System.Linq.Expressions;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationBuilder<T>
	where T : class
{
	private readonly List<KeysetPaginationItem<T>> _items = new();

	internal IReadOnlyList<KeysetPaginationItem<T>> Items => _items;

	public KeysetPaginationBuilder<T> Ascending<TProp>(
		Expression<Func<T, TProp>> propertyExpression)
	{
		return Item(propertyExpression, isDescending: false);
	}

	public KeysetPaginationBuilder<T> Descending<TProp>(
		Expression<Func<T, TProp>> propertyExpression)
	{
		return Item(propertyExpression, isDescending: true);
	}

	private KeysetPaginationBuilder<T> Item<TProp>(
		Expression<Func<T, TProp>> propertyExpression,
		bool isDescending)
	{
		var property = ExpressionHelper.GetPropertyInfoFromMemberAccess(propertyExpression);
		_items.Add(new KeysetPaginationItem<T, TProp>(
			property,
			isDescending));
		return this;
	}
}
