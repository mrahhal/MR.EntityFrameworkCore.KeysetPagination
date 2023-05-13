using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MR.EntityFrameworkCore.KeysetPagination;

public static class KeysetPaginationExtensions
{
	/// <summary>
	/// Paginates using keyset pagination.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
	/// <param name="keysetQueryDefinition">The prebuilt keyset query definition.</param>
	/// <param name="direction">The direction to take. Default is Forward.</param>
	/// <param name="reference">The reference object. Needs to have properties with exact names matching the configured properties. Doesn't necessarily need to be the same type as T.</param>
	/// <returns>An object containing the modified queryable. Can be used with other helper methods related to keyset pagination.</returns>
	/// <exception cref="ArgumentNullException">source or keysetQueryDefinition is null.</exception>
	/// <exception cref="InvalidOperationException">If no columns were registered with the builder.</exception>
	/// <remarks>
	/// Note that calling this method will override any OrderBy calls you have done before.
	/// </remarks>
	public static KeysetPaginationContext<T> KeysetPaginate<T>(
		this IQueryable<T> source,
		KeysetQueryDefinition<T> keysetQueryDefinition,
		KeysetPaginationDirection direction = KeysetPaginationDirection.Forward,
		object? reference = null)
		where T : class
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (keysetQueryDefinition == null) throw new ArgumentNullException(nameof(keysetQueryDefinition));

		return source.KeysetPaginate(keysetQueryDefinition.Columns, direction, reference);
	}

	/// <summary>
	/// Paginates using keyset pagination.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
	/// <param name="builderAction">An action that takes a builder and registers the columns upon which keyset pagination will work.</param>
	/// <param name="direction">The direction to take. Default is Forward.</param>
	/// <param name="reference">The reference object. Needs to have properties with exact names matching the configured properties. Doesn't necessarily need to be the same type as T.</param>
	/// <returns>An object containing the modified queryable. Can be used with other helper methods related to keyset pagination.</returns>
	/// <exception cref="ArgumentNullException">source or builderAction is null.</exception>
	/// <exception cref="InvalidOperationException">If no columns were registered with the builder.</exception>
	/// <remarks>
	/// Note that calling this method will override any OrderBy calls you have done before.
	/// </remarks>
	public static KeysetPaginationContext<T> KeysetPaginate<T>(
		this IQueryable<T> source,
		Action<KeysetPaginationBuilder<T>> builderAction,
		KeysetPaginationDirection direction = KeysetPaginationDirection.Forward,
		object? reference = null)
		where T : class
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (builderAction == null) throw new ArgumentNullException(nameof(builderAction));

		var columns = KeysetQuery.BuildColumns(builderAction);
		return source.KeysetPaginate(columns, direction, reference);
	}

	private static KeysetPaginationContext<T> KeysetPaginate<T>(
		this IQueryable<T> source,
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		object? reference)
		where T : class
	{
		if (source == null) throw new ArgumentNullException(nameof(source));

		if (!columns.Any())
		{
			throw new InvalidOperationException("There should be at least one configured column in the keyset.");
		}

		// Order

		var orderedQuery = columns[0].ApplyOrderBy(source, direction);
		for (var i = 1; i < columns.Count; i++)
		{
			orderedQuery = columns[i].ApplyThenOrderBy(orderedQuery, direction);
		}

		// Filter

		var filteredQuery = orderedQuery.AsQueryable();
		if (reference != null)
		{
			var keysetFilterPredicateLambda = BuildKeysetFilterPredicateExpression(columns, direction, reference);
			filteredQuery = filteredQuery.Where(keysetFilterPredicateLambda);
		}

		return new KeysetPaginationContext<T>(filteredQuery, orderedQuery, columns, direction);
	}

	/// <summary>
	/// Paginates using keyset pagination.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
	/// <param name="builderAction">An action that takes a builder and registers the columns upon which keyset pagination will work.</param>
	/// <param name="direction">The direction to take. Default is Forward.</param>
	/// <param name="reference">The reference object. Needs to have properties with exact names matching the configured properties. Doesn't necessarily need to be the same type as T.</param>
	/// <returns>The modified the queryable.</returns>
	/// <exception cref="ArgumentNullException">source or builderAction is null.</exception>
	/// <exception cref="InvalidOperationException">If no properties were registered with the builder.</exception>
	/// <remarks>
	/// Note that calling this method will override any OrderBy calls you have done before.
	/// </remarks>
	public static IQueryable<T> KeysetPaginateQuery<T>(
		this IQueryable<T> source,
		Action<KeysetPaginationBuilder<T>> builderAction,
		KeysetPaginationDirection direction = KeysetPaginationDirection.Forward,
		object? reference = null)
		where T : class
	{
		return KeysetPaginate(source, builderAction, direction, reference).Query;
	}

	/// <summary>
	/// Paginates using keyset pagination.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
	/// <param name="keysetQueryDefinition">The prebuilt keyset query definition.</param>
	/// <param name="direction">The direction to take. Default is Forward.</param>
	/// <param name="reference">The reference object. Needs to have properties with exact names matching the configured properties. Doesn't necessarily need to be the same type as T.</param>
	/// <returns>The modified the queryable.</returns>
	/// <exception cref="ArgumentNullException">source or builderAction is null.</exception>
	/// <exception cref="InvalidOperationException">If no properties were registered with the builder.</exception>
	/// <remarks>
	/// Note that calling this method will override any OrderBy calls you have done before.
	/// </remarks>
	public static IQueryable<T> KeysetPaginateQuery<T>(
		this IQueryable<T> source,
		KeysetQueryDefinition<T> keysetQueryDefinition,
		KeysetPaginationDirection direction = KeysetPaginationDirection.Forward,
		object? reference = null)
		where T : class
	{
		return KeysetPaginate(source, keysetQueryDefinition, direction, reference).Query;
	}

	/// <summary>
	/// Returns true when there is more data before the list.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <typeparam name="T2">The type of the elements of the data.</typeparam>
	/// <param name="context">The <see cref="KeysetPaginationContext{T}"/> object.</param>
	/// <param name="data">The data list.</param>
	public static Task<bool> HasPreviousAsync<T, T2>(
		this KeysetPaginationContext<T> context,
		IReadOnlyList<T2> data)
		where T : class
	{
		if (data == null) throw new ArgumentNullException(nameof(data));
		if (context == null) throw new ArgumentNullException(nameof(context));

		if (!data.Any())
		{
			return Task.FromResult(false);
		}

		// Get first item and see if there's anything before it.
		var reference = data[0]!;
		return HasAsync(context, KeysetPaginationDirection.Backward, reference);
	}

	/// <summary>
	/// Returns true when there is more data after the list.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <typeparam name="T2">The type of the elements of the data.</typeparam>
	/// <param name="context">The <see cref="KeysetPaginationContext{T}"/> object.</param>
	/// <param name="data">The data list.</param>
	public static Task<bool> HasNextAsync<T, T2>(
		this KeysetPaginationContext<T> context,
		IReadOnlyList<T2> data)
		where T : class
	{
		if (data == null) throw new ArgumentNullException(nameof(data));
		if (context == null) throw new ArgumentNullException(nameof(context));

		if (!data.Any())
		{
			return Task.FromResult(false);
		}

		// Get last item and see if there's anything after it.
		var reference = data[^1]!;
		return HasAsync(context, KeysetPaginationDirection.Forward, reference);
	}

	private static Task<bool> HasAsync<T>(
		this KeysetPaginationContext<T> context,
		KeysetPaginationDirection direction,
		object reference)
		where T : class
	{
		var lambda = BuildKeysetFilterPredicateExpression(
			context.Columns, direction, reference);
		return context.OrderedQuery.AnyAsync(lambda);
	}

	/// <summary>
	/// Ensures the data list is correctly ordered.
	/// Basically applies a reverse on the data if the KeysetPaginate direction was Backward.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <typeparam name="T2">The type of the elements of the data.</typeparam>
	/// <param name="context">The <see cref="KeysetPaginationContext{T}"/> object.</param>
	/// <param name="data">The data list.</param>
	public static void EnsureCorrectOrder<T, T2>(
		this KeysetPaginationContext<T> context,
		List<T2> data)
		where T : class
	{
		if (data == null) throw new ArgumentNullException(nameof(data));
		if (context == null) throw new ArgumentNullException(nameof(context));

		if (context.Direction == KeysetPaginationDirection.Backward)
		{
			data.Reverse();
		}
	}

	private static Expression<Func<T, bool>> BuildKeysetFilterPredicateExpression<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		object reference)
		where T : class
	{
		return KeysetFilterPredicateStrategy.Default.BuildKeysetFilterPredicateExpression(
			columns,
			direction,
			reference);
	}
}
