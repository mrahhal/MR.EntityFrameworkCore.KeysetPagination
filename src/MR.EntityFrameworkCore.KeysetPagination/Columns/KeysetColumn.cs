using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MR.EntityFrameworkCore.KeysetPagination.Columns
{
	public abstract class KeysetColumn<TEntity> : IKeysetColumn<TEntity>
	where TEntity : class
	{
		private static readonly Func<string, string> DefaultIncompatibleMessageFunc = propertyName => $"A matching property '{propertyName}' was not found on this object";

		public KeysetColumn(
			bool isDescending)
		{
			_isDescending = isDescending;
		}

		protected bool _isDescending { get; set; }
		public bool IsDescending => _isDescending;

		private string GetOrderByCommand(KeysetPaginationDirection direction)
		{
			var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
			return isDescending ? "OrderByDescending" : "OrderBy";
		}
		private string GetThenByCommand(KeysetPaginationDirection direction)
		{
			var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
			return isDescending ? "ThenByDescending" : "ThenBy";
		}
		private MethodInfo GetOrderByMethodFromName(string orderByCommand)
		{
			var queryableType = typeof(Queryable);
			var method = queryableType.GetMethods()
				.Where(m => m.Name == orderByCommand && m.IsGenericMethodDefinition)
				.Where(m =>
				{
					var parameters = m.GetParameters().ToList();
					return parameters.Count == 2;
				}).Single();

			return method;
		}

		protected virtual LambdaExpression MakeMemberAccessLambda()
		{
			// x => x.Property
			// ---------------

			// x => x.Nested.Property
			// ----------------------

			// x => x.Nested.Property.Json.GetProperty("zz").GetInt32()
			// ----------------------

			// x =>
			var param = Expression.Parameter(typeof(TEntity), "x");

			// x.Property
			var propertyMemberAccess = MakeMemberAccessExpression(param);

			return Expression.Lambda(propertyMemberAccess, param);
		}

		public abstract Expression MakeMemberAccessExpression(ParameterExpression param);
		public abstract object ObtainValue(object reference);

		public virtual IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> query, KeysetPaginationDirection direction)
		{
			var accessExpression = MakeMemberAccessLambda();
			var methodName = GetOrderByCommand(direction);
			var genericMethod = GetOrderByMethodFromName(methodName).MakeGenericMethod(typeof(TEntity), accessExpression.ReturnType);
			var finalQuery = genericMethod.Invoke(genericMethod, new object[] { query, accessExpression });
			if (null == finalQuery)
			{
				throw new InvalidOperationException($"Unable to call '{methodName}'");
			}
			return (IOrderedQueryable<TEntity>)finalQuery;
		}

		public virtual IOrderedQueryable<TEntity> ApplyThenOrderBy(IOrderedQueryable<TEntity> query, KeysetPaginationDirection direction)
		{
			var accessExpression = MakeMemberAccessLambda();
			var methodName = GetThenByCommand(direction);
			var genericMethod = GetOrderByMethodFromName(methodName).MakeGenericMethod(typeof(TEntity), accessExpression.ReturnType);
			var finalQuery = genericMethod.Invoke(genericMethod, new object[] { query, accessExpression });
			if (null == finalQuery)
			{
				throw new InvalidOperationException($"Unable to call '{methodName}'");
			}
			return (IOrderedQueryable<TEntity>)finalQuery;
		}


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
}
