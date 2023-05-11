using System.Linq.Expressions;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationBuilder<T>
	where T : class
{
	private readonly List<KeysetColumn<T>> _columns = new();

	internal IReadOnlyList<KeysetColumn<T>> Columns => _columns;

	public KeysetPaginationBuilder<T> Ascending<TColumn>(
		Expression<Func<T, TColumn>> columnExpression)
	{
		return ConfigureColumn(columnExpression, isDescending: false);
	}

	public KeysetPaginationBuilder<T> Descending<TColumn>(
		Expression<Func<T, TColumn>> columnExpression)
	{
		return ConfigureColumn(columnExpression, isDescending: true);
	}

	private KeysetPaginationBuilder<T> ConfigureColumn<TColumn>(
		Expression<Func<T, TColumn>> columnExpression,
		bool isDescending)
	{
		_columns.Add(new KeysetColumn<T, TColumn>(isDescending, columnExpression));
		return this;
	}
}
