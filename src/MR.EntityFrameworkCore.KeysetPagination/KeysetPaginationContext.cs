using MR.EntityFrameworkCore.KeysetPagination.Columns;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationContext<T>
	where T : class
{
	internal KeysetPaginationContext(
		IQueryable<T> query,
		IOrderedQueryable<T> orderedQuery,
		IReadOnlyList<IKeysetColumn<T>> columns,
		KeysetPaginationDirection direction)
	{
		Query = query;
		OrderedQuery = orderedQuery;
		Columns = columns;
		Direction = direction;
	}

	/// <summary>
	/// The final query.
	/// </summary>
	public IQueryable<T> Query { get; }

	/// <summary>
	/// This query includes only the order instructions without the predicate.
	/// </summary>
	public IQueryable<T> OrderedQuery { get; }

	/// <summary>
	/// The direction with which KeysetPaginate was called.
	/// </summary>
	public KeysetPaginationDirection Direction { get; }

	internal IReadOnlyList<IKeysetColumn<T>> Columns { get; }
}
