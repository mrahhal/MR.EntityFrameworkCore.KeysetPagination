using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

/// <summary>
/// Represents a configured keyset column.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
internal abstract class KeysetColumn<T>
	where T : class
{
	private static readonly Func<string, string> DefaultIncompatibleMessageFunc = propertyName => $"A matching property '{propertyName}' was not found on this object";

	public KeysetColumn(
		bool isDescending)
	{
		IsDescending = isDescending;
	}

	public bool IsDescending { get; }

	/// <summary>
	/// The property that represents the actual column.
	/// </summary>
	public abstract PropertyInfo Property { get; }

	public abstract MemberExpression MakeMemberAccessExpression(ParameterExpression param);

	public abstract IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction);

	public abstract IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction);

	public abstract object ObtainValue(object reference);

	protected Exception CreateIncompatibleObjectException(string propertyName, Func<string, string>? messageFunc = null) =>
		new KeysetPaginationIncompatibleObjectException(
			$"{(messageFunc ?? DefaultIncompatibleMessageFunc)(propertyName)}");

#pragma warning disable IDE0060 // Remove unused parameter
	protected object CheckAndReturn(object? value, string propertyName)
#pragma warning restore IDE0060 // Remove unused parameter
	{
		// We don't want to throw on nulls because even though we don't support it in the keyset,
		// we want the analyzer to take care of warning the user to allow them the flexibility of suppressing it.

		return value!;
	}
}

/// <summary>
/// A <see cref="KeysetColumn{T}"/> for a simple property access: x => x.Column
/// </summary>
internal class KeysetColumnSimple<T, TProp> : KeysetColumn<T>
	where T : class
{
	public KeysetColumnSimple(
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
			throw CreateIncompatibleObjectException(propertyName);
		}

		return CheckAndReturn(value, propertyName);
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

/// <summary>
/// A <see cref="KeysetColumn{T}"/> for a nested property access: x => x.Nested.Column
/// </summary>
internal class KeysetColumnNested<T, TProp> : KeysetColumn<T>
	where T : class
{
	public KeysetColumnNested(
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
		var lastPropertyName = string.Empty;
		for (var i = 0; i < Properties.Count; i++)
		{
			var property = Properties[i];
			// Suppression: This can't be null. It can only become null when we're already breaking out of the loop.
			var accessor = Accessor.Obtain(lastValue!.GetType());
			var propertyName = lastPropertyName = property.Name;
			if (!accessor.TryGetPropertyValue(lastValue, propertyName, out var value))
			{
				throw CreateIncompatibleObjectException(propertyName);
			}

			// An intermediate being null means it probably wasn't loaded, so let's throw a clear error.
			if (i != Properties.Count - 1 && value == null)
			{
				// Chain might have not been loaded properly.
				throw CreateIncompatibleObjectException(
					propertyName,
					propertyName => $"A nested property had a null in the chain ('{propertyName}'). Did you properly load the chain?");
			}

			lastValue = value;
		}

		return CheckAndReturn(lastValue, lastPropertyName);
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
