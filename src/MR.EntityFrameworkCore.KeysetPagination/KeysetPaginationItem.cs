using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal abstract class KeysetPaginationItem<T>
	where T : class
{
	public KeysetPaginationItem(
		bool isDescending)
	{
		IsDescending = isDescending;
	}

	public bool IsDescending { get; }

	/// <summary>
	/// The property that represents the keyset column of this item.
	/// </summary>
	public abstract PropertyInfo Property { get; }

	public abstract MemberExpression MakeMemberAccessExpression(ParameterExpression param);

	public abstract IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction);

	public abstract IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction);

	public abstract object ObtainValue(object reference);
}

internal class KeysetPaginationItemSimple<T, TProp> : KeysetPaginationItem<T>
	where T : class
{
	public KeysetPaginationItemSimple(
		PropertyInfo property,
		bool isDescending)
		: base(isDescending)
	{
		Property = property;
	}

	public override PropertyInfo Property { get; }

	public override IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction)
	{
		var accessExpression = MakeMemberAccessLambda<TProp>();
		var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
		return isDescending ? query.OrderByDescending(accessExpression) : query.OrderBy(accessExpression);
	}

	public override IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction)
	{
		var accessExpression = MakeMemberAccessLambda<TProp>();
		var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
		return isDescending ? query.ThenByDescending(accessExpression) : query.ThenBy(accessExpression);
	}

	public override MemberExpression MakeMemberAccessExpression(ParameterExpression param)
	{
		return Expression.MakeMemberAccess(param, Property);
	}

	public override object ObtainValue(object reference)
	{
		var accessor = Accessor.Obtain(reference.GetType());

		var propertyName = Property.Name;
		if (!accessor.TryGetPropertyValue(reference, propertyName, out var value))
		{
			throw new KeysetPaginationIncompatibleObjectException($"Property '{propertyName}' not found on this object.");
		}

		return value;
	}

	private Expression<Func<T, TKey>> MakeMemberAccessLambda<TKey>()
	{
		// x => x.Property
		// ---------------

		// x =>
		var param = Expression.Parameter(typeof(T), "x");

		// x.Property
		var propertyMemberAccess = MakeMemberAccessExpression(param);

		return Expression.Lambda<Func<T, TKey>>(propertyMemberAccess, param);
	}
}

internal class KeysetPaginationItemNested<T, TProp> : KeysetPaginationItem<T>
	where T : class
{
	public KeysetPaginationItemNested(
		List<PropertyInfo> properties,
		bool isDescending)
		: base(isDescending)
	{
		Debug.Assert(properties.Count > 1, "Expected this to be a chain of nested properties (more than 1 property).");

		Properties = properties;
		Property = properties[^1];
	}

	public IReadOnlyList<PropertyInfo> Properties { get; }

	public override PropertyInfo Property { get; }

	public override IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction)
	{
		var accessExpression = MakeMemberAccessLambda<TProp>();
		var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
		return isDescending ? query.OrderByDescending(accessExpression) : query.OrderBy(accessExpression);
	}

	public override IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction)
	{
		var accessExpression = MakeMemberAccessLambda<TProp>();
		var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
		return isDescending ? query.ThenByDescending(accessExpression) : query.ThenBy(accessExpression);
	}

	public override MemberExpression MakeMemberAccessExpression(ParameterExpression param)
	{
		// x.Nested.Property
		// -----------------

		var resultingExpression = default(MemberExpression);
		foreach (var prop in Properties)
		{
			var chainAnchor = resultingExpression ?? (Expression)param;
			resultingExpression = Expression.MakeMemberAccess(chainAnchor, prop);
		}

		return resultingExpression!;
	}

	public override object ObtainValue(object reference)
	{
		var lastValue = reference;
		foreach (var prop in Properties)
		{
			var accessor = Accessor.Obtain(lastValue.GetType());
			var propertyName = prop.Name;
			if (!accessor.TryGetPropertyValue(lastValue, propertyName, out var value))
			{
				throw new KeysetPaginationIncompatibleObjectException($"Property '{propertyName}' not found on this object.");
			}
			lastValue = value;
		}

		return lastValue;
	}

	private Expression<Func<T, TKey>> MakeMemberAccessLambda<TKey>()
	{
		// x => x.Nested.Property
		// ----------------------

		// x =>
		var param = Expression.Parameter(typeof(T), "x");

		// x.Nested.Property
		var propertyMemberAccess = MakeMemberAccessExpression(param);

		return Expression.Lambda<Func<T, TKey>>(propertyMemberAccess, param);
	}
}
