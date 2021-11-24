using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal abstract class KeysetPaginationItem<T>
	where T : class
{
	public KeysetPaginationItem(
		PropertyInfo property,
		bool isDescending)
	{
		Property = property;
		IsDescending = isDescending;
	}

	public PropertyInfo Property { get; }

	public bool IsDescending { get; }

	public abstract IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction);

	public abstract IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction);
}

internal class KeysetPaginationItem<T, TProp> : KeysetPaginationItem<T>
	where T : class
{
	public KeysetPaginationItem(
		PropertyInfo property,
		bool isDescending)
		: base(property, isDescending)
	{
	}

	public override IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction)
	{
		return OrderBy(query, direction, this);
	}

	public override IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction)
	{
		return ThenOrderBy(query, direction, this);
	}

	private static IOrderedQueryable<T> OrderBy(IQueryable<T> query, KeysetPaginationDirection direction, KeysetPaginationItem<T> item)
	{
		var accessExpression = MakeMemberAccessLambda<TProp>(item.Property);
		var isDescending = item.IsDescending;
		if (direction == KeysetPaginationDirection.Backward)
		{
			isDescending = !isDescending;
		}
		return isDescending ? query.OrderByDescending(accessExpression) : query.OrderBy(accessExpression);
	}

	private static IOrderedQueryable<T> ThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction, KeysetPaginationItem<T> item)
	{
		var accessExpression = MakeMemberAccessLambda<TProp>(item.Property);
		var isDescending = item.IsDescending;
		if (direction == KeysetPaginationDirection.Backward)
		{
			isDescending = !isDescending;
		}
		return isDescending ? query.ThenBy(accessExpression) : query.ThenByDescending(accessExpression);
	}

	private static Expression<Func<T, TKey>> MakeMemberAccessLambda<TKey>(PropertyInfo property)
	{
		// x => x.Property
		// ---------------

		// x =>
		var param = Expression.Parameter(typeof(T), "x");

		// x.Property
		var propertyMemberAccess = Expression.MakeMemberAccess(param, property);

		return Expression.Lambda<Func<T, TKey>>(propertyMemberAccess, param);
	}
}
