using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MR.EntityFrameworkCore.KeysetPagination;

public static class KeysetPaginationExtensions
{
	private static readonly IReadOnlyDictionary<Type, MethodInfo> TypeToCompareToMethod = new Dictionary<Type, MethodInfo>
	{
		{ typeof(string), GetCompareToMethod(typeof(string)) },
		{ typeof(Guid), GetCompareToMethod(typeof(Guid)) },
	};
	private static readonly ConstantExpression ConstantExpression0 = Expression.Constant(0);

	/// <summary>
	/// Paginates using keyset pagination.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
	/// <param name="builderAction">An action that takes a builder and registers the columns upon which keyset pagination will work.</param>
	/// <param name="direction">The direction to take. Default is Forward.</param>
	/// <param name="reference">The reference object. Needs to have properties with exact names matching the configured properties. Doesn't necessarily need to be the same type as T.</param>
	/// <returns>An object containing the modified queryable. Can be used with other helper methods related to keyset pagination.</returns>
	/// <exception cref="ArgumentNullException">source or builderAction is null.</exception>
	/// <exception cref="InvalidOperationException">If no properties were registered with the builder.</exception>
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
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (builderAction == null)
		{
			throw new ArgumentNullException(nameof(builderAction));
		}

		var builder = new KeysetPaginationBuilder<T>();
		builderAction(builder);
		var items = builder.Items;

		if (!items.Any())
		{
			throw new InvalidOperationException("There should be at least one property you're acting on.");
		}

		// Order

		var orderedQuery = items[0].ApplyOrderBy(source, direction);
		for (var i = 1; i < items.Count; i++)
		{
			orderedQuery = items[i].ApplyThenOrderBy(orderedQuery, direction);
		}

		// Filter

		var filteredQuery = orderedQuery.AsQueryable();
		if (reference != null)
		{
			var keysetFilterPredicateLambda = BuildKeysetFilterPredicateExpression(items, direction, reference);
			filteredQuery = filteredQuery.Where(keysetFilterPredicateLambda);
		}

		return new KeysetPaginationContext<T>(filteredQuery, orderedQuery, items);
	}

	/// <summary>
	/// Paginates using keyset pagination.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
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
	/// Returns true when there is more data before the list.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <typeparam name="T2">The type of the elements of the list.</typeparam>
	/// <param name="context">The <see cref="KeysetPaginationContext{T}"/> object.</param>
	/// <param name="data">The data list.</param>
	public static Task<bool> HasPreviousAsync<T, T2>(
		this KeysetPaginationContext<T> context,
		List<T2> data)
		where T : class
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		if (!data.Any())
		{
			return Task.FromResult(false);
		}

		var reference = data.First()!;
		return HasAsync(context, KeysetPaginationDirection.Backward, reference);
	}

	/// <summary>
	/// Returns true when there is more data after the list.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <typeparam name="T2">The type of the elements of the list.</typeparam>
	/// <param name="context">The <see cref="KeysetPaginationContext{T}"/> object.</param>
	/// <param name="data">The data list.</param>
	public static Task<bool> HasNextAsync<T, T2>(
		this KeysetPaginationContext<T> context,
		List<T2> data)
		where T : class
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		if (!data.Any())
		{
			return Task.FromResult(false);
		}

		var reference = data.Last()!;
		return HasAsync(context, KeysetPaginationDirection.Forward, reference);
	}

	private static Task<bool> HasAsync<T>(
		this KeysetPaginationContext<T> context,
		KeysetPaginationDirection direction,
		object reference)
		where T : class
	{
		var lambda = BuildKeysetFilterPredicateExpression(
			context.Items, direction, reference);
		return context.OrderedQuery.AnyAsync(lambda);
	}

	private static List<object> GetValues<T>(
		IReadOnlyList<KeysetPaginationItem<T>> items,
		object reference)
		where T : class
	{
		var accessor = Accessor.Obtain(reference.GetType());
		var referenceValues = new List<object>(capacity: items.Count);
		foreach (var item in items)
		{
			var value = accessor.GetPropertyValue(reference, item.Property.Name);
			referenceValues.Add(value);
		}
		return referenceValues;
	}

	private static Expression<Func<T, bool>> BuildKeysetFilterPredicateExpression<T>(
		IReadOnlyList<KeysetPaginationItem<T>> items,
		KeysetPaginationDirection direction,
		object reference)
		where T : class
	{
		// A composite keyset pagination in sql looks something like this:
		//   (x, y, ...) > (a, b, ...)
		// Where, x/y/... represent the column and a/b/... represent the reference's respective values.
		//
		// In sql standard this syntax is called "row values". Check here: https://use-the-index-luke.com/sql/partial-results/fetch-next-page#sb-row-values
		// Unfortunately, not a lot of databases support this properly.
		// Further, if we were to use this we would somehow need EFCore to recognise it and transform it
		// perhaps by using a new DbFunction (https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbfunctions).
		//
		// So, we're going to implement it ourselves (less efficient when compared
		// to a proper db implementation for row values but will still do the job).
		//
		// The generalized expression for this in pseudocode is:
		//   (x > a) OR
		//   (x = a AND y > b) OR
		//   (x = a AND y = b AND z > c) OR...
		//
		// Of course, this will be a bit more complex when ASC and DESC are mixed.
		// Assume x is ASC, y is DESC, and z is ASC:
		//   (x > a) OR
		//   (x = a AND y < b) OR
		//   (x = a AND y = b AND z > c) OR...
		//
		// An optimization is to include an additional redundant wrapping clause for the 1st column when there are
		// more than one column we're acting on, which would allow the db to use it as an access predicate on the 1st column.
		// See here: https://use-the-index-luke.com/sql/partial-results/fetch-next-page#sb-equivalent-logic

		var referenceValues = GetValues(items, reference);

		var firstMemberAccessExpression = default(MemberExpression);
		var firstReferenceValueExpression = default(ConstantExpression);

		// entity =>
		var param = Expression.Parameter(typeof(T), "entity");

		var orExpression = default(BinaryExpression)!;
		var innerLimit = 1;
		// This loop compounds the outer OR expressions.
		for (var i = 0; i < items.Count; i++)
		{
			var andExpression = default(BinaryExpression)!;

			// This loop compounds the inner AND expressions.
			// innerLimit implicitly grows from 1 to items.Count by each iteration.
			for (var j = 0; j < innerLimit; j++)
			{
				var isOrLast = j + 1 == innerLimit;
				var item = items[j];
				var memberAccess = Expression.MakeMemberAccess(param, item.Property);
				var referenceValueExpression = Expression.Constant(referenceValues[j]);

				if (firstMemberAccessExpression == null)
				{
					// This might be used later on in an optimization.
					firstMemberAccessExpression = memberAccess;
					firstReferenceValueExpression = referenceValueExpression;
				}

				BinaryExpression innerExpression;
				if (!isOrLast)
				{
					innerExpression = Expression.Equal(memberAccess, referenceValueExpression);
				}
				else
				{
					var compare = GetComparisonExpressionToApply(direction, item, orEqual: false);
					innerExpression = MakeComparisonExpression(
						item,
						memberAccess, referenceValueExpression,
						compare);
				}

				andExpression = andExpression == null ? innerExpression : Expression.And(andExpression, innerExpression);
			}

			orExpression = orExpression == null ? andExpression : Expression.Or(orExpression, andExpression);

			innerLimit++;
		}

		var finalExpression = orExpression;
		if (items.Count > 1)
		{
			// Implement the optimization that allows an access predicate on the 1st column.
			// This is done by generating the following expression:
			//   (x >=|<= a) AND (previous generated expression)
			//
			// This effectively adds a redudant clause on the 1st column, but it's a clause all dbs
			// understand and can use as an access predicate (most commonly when the column is indexed).

			var firstItem = items[0];
			var compare = GetComparisonExpressionToApply(direction, firstItem, orEqual: true);
			var accessPredicateClause = MakeComparisonExpression(
				firstItem,
				firstMemberAccessExpression!, firstReferenceValueExpression!,
				compare);
			finalExpression = Expression.And(accessPredicateClause, finalExpression);
		}

		return Expression.Lambda<Func<T, bool>>(finalExpression, param);
	}

	private static BinaryExpression MakeComparisonExpression<T>(
		KeysetPaginationItem<T> item,
		MemberExpression memberAccess, ConstantExpression referenceValue,
		Func<Expression, Expression, BinaryExpression> compare)
		where T : class
	{
		var propertyType = item.Property.PropertyType;
		if (TypeToCompareToMethod.TryGetValue(propertyType, out var compareToMethod))
		{
			// LessThan/GreaterThan operators are not valid for some types such as strings and guids.
			// We use the CompareTo method on these types instead.

			// entity.Property.CompareTo(constant) >|< 0
			// -----------------------------------------

			// entity.Property.CompareTo(constant)
			var methodCallExpression = Expression.Call(memberAccess, compareToMethod, referenceValue);

			// >|< 0
			return compare(methodCallExpression, ConstantExpression0);
		}
		else
		{
			return compare(memberAccess, referenceValue);
		}
	}

	private static Func<Expression, Expression, BinaryExpression> GetComparisonExpressionToApply<T>(
		KeysetPaginationDirection direction, KeysetPaginationItem<T> item, bool orEqual)
		where T : class
	{
		var greaterThan = direction switch
		{
			KeysetPaginationDirection.Forward when !item.IsDescending => true,
			KeysetPaginationDirection.Forward when item.IsDescending => false,
			KeysetPaginationDirection.Backward when !item.IsDescending => false,
			KeysetPaginationDirection.Backward when item.IsDescending => true,
			_ => throw new NotImplementedException(),
		};

		return orEqual ?
			(greaterThan ? Expression.GreaterThanOrEqual : Expression.LessThanOrEqual) :
			(greaterThan ? Expression.GreaterThan : Expression.LessThan);
	}

	private static MethodInfo GetCompareToMethod(Type type)
	{
		var methodInfo = type.GetTypeInfo().GetMethod(nameof(string.CompareTo), new Type[] { type });
		if (methodInfo == null)
		{
			throw new InvalidOperationException($"Didn't find a CompareTo method on type {type.Name}.");
		}

		return methodInfo;
	}
}
