using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MR.EntityFrameworkCore.KeysetPagination
{
	public static class KeysetPaginationExtensions
	{
		private static readonly MethodInfo StringCompareToMethod;
		private static readonly ConstantExpression ConstantExpression0 = Expression.Constant(0);

		static KeysetPaginationExtensions()
		{
			StringCompareToMethod = typeof(string).GetTypeInfo().GetMethod("CompareTo", new Type[] { typeof(string) });
		}

		/// <summary>
		/// Paginates using keyset pagination.
		/// </summary>
		/// <typeparam name="T">The type of the elements of source.</typeparam>
		/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
		/// <param name="builderAction">An action that takes a builder and registers the columns upon which keyset pagination will work.</param>
		/// <param name="reference">The reference entity. This can only be null when direction is also null.</param>
		/// <param name="direction">The direction to take. This can only be null when reference is also null.</param>
		/// <returns>An object containing the modified queryable. Can be used with other helper methods related to keyset pagination.</returns>
		/// <exception cref="ArgumentNullException">source or builderAction is null.</exception>
		/// <exception cref="ArgumentException">One of <paramref name="reference"/> or <paramref name="direction"/> is null.</exception>
		/// <exception cref="InvalidOperationException">If no properties were registered with the builder.</exception>
		/// <remarks>
		/// Note that calling this method will override any OrderBy calls you have done before.
		/// It's also important that you call this the very last thing in the queryable call chain
		/// to ensure this works correctly.
		/// </remarks>
		public static KeysetPaginationContext<T> KeysetPaginate<T>(
			this IQueryable<T> source,
			Action<KeysetPaginationBuilder<T>> builderAction,
			T reference = null,
			KeysetPaginationReferenceDirection? direction = null)
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
			if (reference != null && direction == null)
			{
				throw new ArgumentException(nameof(direction), "direction is required when there's a reference.");
			}
			if (reference == null && direction != null)
			{
				throw new ArgumentException(nameof(direction), "direction doesn't make sense when there's no reference.");
			}

			var builder = new KeysetPaginationBuilder<T>();
			builderAction(builder);
			var items = builder.Items;

			if (!items.Any())
			{
				throw new InvalidOperationException("There should be at least one property you're acting on.");
			}

			// Order

			var orderedQuery = items[0].ApplyOrderBy(source);
			for (var i = 1; i < items.Count; i++)
			{
				orderedQuery = items[i].ApplyThenOrderBy(orderedQuery);
			}

			// Predicate

			var predicateQuery = orderedQuery.AsQueryable();
			if (reference != null)
			{
				var keysetPredicateLambda = BuildKeysetPredicateExpression(items, reference, direction.Value);
				predicateQuery = predicateQuery.Where(keysetPredicateLambda);
			}

			return new KeysetPaginationContext<T>(predicateQuery, orderedQuery, items);
		}

		/// <summary>
		/// Paginates using keyset pagination.
		/// </summary>
		/// <typeparam name="T">The type of the elements of source.</typeparam>
		/// <param name="source">An <see cref="IQueryable{T}"/> to paginate.</param>
		/// <param name="builderAction">An action that takes a builder and registers the columns upon which keyset pagination will work.</param>
		/// <param name="reference">The reference entity. This can only be null when direction is also null.</param>
		/// <param name="direction">The direction to take. This can only be null when reference is also null.</param>
		/// <returns>The modified the queryable.</returns>
		/// <exception cref="ArgumentNullException">source or builderAction is null.</exception>
		/// <exception cref="ArgumentException">One of <paramref name="reference"/> or <paramref name="direction"/> is null.</exception>
		/// <exception cref="InvalidOperationException">If no properties were registered with the builder.</exception>
		/// <remarks>
		/// Note that calling this method will override any OrderBy calls you have done before.
		/// It's also important that you call this the very last thing in the queryable call chain
		/// to ensure this works correctly.
		/// </remarks>
		public static IQueryable<T> KeysetPaginateQuery<T>(
			this IQueryable<T> source,
			Action<KeysetPaginationBuilder<T>> builderAction,
			T reference = null,
			KeysetPaginationReferenceDirection? direction = null)
			where T : class
		{
			return KeysetPaginate(source, builderAction, reference, direction).Query;
		}

		public static Task<bool> HasPreviousAsync<T>(
			this KeysetPaginationContext<T> context,
			List<T> source)
			where T : class
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			var reference = source.First();
			var lambda = BuildKeysetPredicateExpression(
				context.Items, reference, KeysetPaginationReferenceDirection.Before);
			return context.OrderedQuery.AnyAsync(lambda);
		}

		public static Task<bool> HasNextAsync<T>(
			this KeysetPaginationContext<T> context,
			List<T> source)
			where T : class
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			var reference = source.Last();
			var lambda = BuildKeysetPredicateExpression(
				context.Items, reference, KeysetPaginationReferenceDirection.After);
			return context.OrderedQuery.AnyAsync(lambda);
		}

		private static Expression<Func<T, bool>> BuildKeysetPredicateExpression<T>(
			IReadOnlyList<KeysetPaginationItem<T>> items,
			T reference,
			KeysetPaginationReferenceDirection direction)
			where T : class
		{
			// A composite keyset pagination in sql looks something like this:
			//   (x, y, ...) > (a, b, ...)
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

			var referenceValues = new List<object>(capacity: items.Count);
			foreach (var item in items)
			{
				referenceValues.Add(item.Property.GetValue(reference));
			}

			// entity =>
			var param = Expression.Parameter(typeof(T), "entity");

			var orExpression = default(BinaryExpression);
			var innerLimit = 1;
			// This loop compounds the outer OR expressions.
			for (var i = 0; i < items.Count; i++)
			{
				var andExpression = default(BinaryExpression);

				// This loop compounds the inner AND expressions.
				// innerLimit implicitly grows from 1 to items.Count by each iteration.
				for (var j = 0; j < innerLimit; j++)
				{
					var isLast = j + 1 == innerLimit;
					var item = items[j];
					var referenceValueExpression = Expression.Constant(referenceValues[j]);
					var memberAccess = Expression.MakeMemberAccess(param, item.Property);

					BinaryExpression innerExpression;
					if (!isLast)
					{
						innerExpression = Expression.Equal(memberAccess, referenceValueExpression);
					}
					else
					{
						var greaterThan = direction switch
						{
							KeysetPaginationReferenceDirection.After when !item.IsDescending => true,
							KeysetPaginationReferenceDirection.After when item.IsDescending => false,
							KeysetPaginationReferenceDirection.Before when !item.IsDescending => false,
							KeysetPaginationReferenceDirection.Before when item.IsDescending => true,
							_ => throw new NotImplementedException(),
						};

						var propertyType = item.Property.PropertyType;
						if (propertyType == typeof(string))
						{
							// GreaterThan/LessThan operators are not valid for strings.
							// We use string.CompareTo instead.

							// entity.Property.CompareTo(constant) >|< 0
							// -----------------------------------------

							// entity.Property.CompareTo(constant)
							var methodCallExpression = Expression.Call(memberAccess, StringCompareToMethod, referenceValueExpression);

							innerExpression = greaterThan ?
								Expression.GreaterThan(methodCallExpression, ConstantExpression0) :
								Expression.LessThan(methodCallExpression, ConstantExpression0);
						}
						else
						{
							innerExpression = greaterThan ?
								Expression.GreaterThan(memberAccess, referenceValueExpression) :
								Expression.LessThan(memberAccess, referenceValueExpression);
						}
					}

					andExpression = andExpression == null ? innerExpression : Expression.And(andExpression, innerExpression);
				}

				orExpression = orExpression == null ? andExpression : Expression.Or(orExpression, andExpression);

				innerLimit++;
			}

			return Expression.Lambda<Func<T, bool>>(orExpression, param);
		}

		internal static Expression<Func<T, TKey>> MakeMemberAccessLambda<T, TKey>(PropertyInfo property)
			where T : class
		{
			// x => x.Property
			// ---------------

			// x =>
			var param = Expression.Parameter(typeof(T), "x");

			// x.Property
			var propertyMemberAccess = Expression.MakeMemberAccess(param, property);

			return Expression.Lambda<Func<T, TKey>>(propertyMemberAccess, param);
		}

		public class KeysetPaginationBuilder<T>
			where T : class
		{
			private readonly List<KeysetPaginationItem<T>> _items = new List<KeysetPaginationItem<T>>();

			public IReadOnlyList<KeysetPaginationItem<T>> Items => _items;

			public KeysetPaginationBuilder<T> Ascending<TProp>(
				Expression<Func<T, TProp>> propertyExpression)
			{
				return Item(propertyExpression, false);
			}

			public KeysetPaginationBuilder<T> Descending<TProp>(
				Expression<Func<T, TProp>> propertyExpression)
			{
				return Item(propertyExpression, true);
			}

			private KeysetPaginationBuilder<T> Item<TProp>(
				Expression<Func<T, TProp>> propertyExpression,
				bool isDescending)
			{
				var property = ExpressionHelper.GetPropertyInfoFromMemberAccess(propertyExpression);
				_items.Add(new KeysetPaginationItem<T, TProp>
				{
					Property = property,
					IsDescending = isDescending,
				});
				return this;
			}
		}

		public class KeysetPaginationItem<T, TProp> : KeysetPaginationItem<T>
			where T : class
		{
			public override IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query)
			{
				return OrderBy(query, this);
			}

			public override IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query)
			{
				return ThenOrderBy(query, this);
			}

			private static IOrderedQueryable<T> OrderBy(IQueryable<T> query, KeysetPaginationItem<T> item)
			{
				var accessExpression = MakeMemberAccessLambda<T, TProp>(item.Property);
				return item.IsDescending ? query.OrderByDescending(accessExpression) : query.OrderBy(accessExpression);
			}

			private static IOrderedQueryable<T> ThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationItem<T> item)
			{
				var accessExpression = MakeMemberAccessLambda<T, TProp>(item.Property);
				return item.IsDescending ? query.ThenBy(accessExpression) : query.ThenByDescending(accessExpression);
			}
		}

		public abstract class KeysetPaginationItem<T>
			where T : class
		{
			public PropertyInfo Property { get; set; }

			public bool IsDescending { get; set; }

			public abstract IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query);

			public abstract IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query);
		}
	}

	public class KeysetPaginationContext<T>
		where T : class
	{
		public KeysetPaginationContext(
			IQueryable<T> query,
			IOrderedQueryable<T> orderedQuery,
			IReadOnlyList<KeysetPaginationExtensions.KeysetPaginationItem<T>> items)
		{
			Query = query;
			OrderedQuery = orderedQuery;
			Items = items;
		}

		/// <summary>
		/// The final query.
		/// </summary>
		public IQueryable<T> Query { get; }

		/// <summary>
		/// This query includes only the order instructions without the predicate.
		/// </summary>
		public IQueryable<T> OrderedQuery { get; }

		public IReadOnlyList<KeysetPaginationExtensions.KeysetPaginationItem<T>> Items { get; }
	}
}
