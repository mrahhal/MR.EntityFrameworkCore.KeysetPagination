using System.Linq.Expressions;

namespace MR.EntityFrameworkCore.KeysetPagination;

/// <summary>
/// Builder for a keyset definition.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public class KeysetPaginationBuilder<T>
	where T : class
{
	private readonly List<KeysetColumn<T>> _columns = new();

	internal IReadOnlyList<KeysetColumn<T>> Columns => _columns;

	/// <summary>
	/// Configures an ascending column as part of the keyset.
	/// </summary>
	public KeysetPaginationBuilder<T> Ascending<TColumn>(
		Expression<Func<T, TColumn>> columnExpression)
	{
		return ConfigureColumn(columnExpression, isDescending: false);
	}

	/// <summary>
	/// Configures a descending column as part of the keyset.
	/// </summary>
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
