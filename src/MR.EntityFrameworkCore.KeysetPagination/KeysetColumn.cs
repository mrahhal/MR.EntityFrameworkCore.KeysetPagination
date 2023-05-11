using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal abstract class KeysetColumn<T>
{
	public KeysetColumn(
		bool isDescending,
		LambdaExpression lambdaExpression)
	{
		IsDescending = isDescending;
		LambdaExpression = lambdaExpression;
	}

	public bool IsDescending { get; }

	public LambdaExpression LambdaExpression { get; }

	/// <summary>
	/// Gets the type of the column.
	/// </summary>
	public Type Type => LambdaExpression.Body.Type;

	public abstract Expression MakeAccessExpression(ParameterExpression parameter);

	public abstract IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction);

	public abstract IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction);

	public abstract object ObtainValue<TReference>(TReference reference);
}

/// <summary>
/// Represents a keyset column.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
/// <typeparam name="TColumn">The type of the column.</typeparam>
internal sealed class KeysetColumn<T, TColumn> : KeysetColumn<T>
	where T : class
{
	private readonly ConcurrentDictionary<Type, Func<object, TColumn>> _typeToCompiledAccessMap = new();

	public KeysetColumn(
		bool isDescending,
		Expression<Func<T, TColumn>> expression)
		: base(isDescending, expression)
	{
	}

	public new Expression<Func<T, TColumn>> LambdaExpression => (Expression<Func<T, TColumn>>)base.LambdaExpression;

	public override Expression MakeAccessExpression(ParameterExpression parameter)
	{
		return MakeAccessLambdaExpression(parameter).Body;
	}

	/// <summary>
	/// Makes an access lambda expression for this column.
	/// Uses the given parameter if it is not null, otherwise creates and uses a new parameter.
	/// </summary>
	private Expression<Func<T, TColumn>> MakeAccessLambdaExpression(ParameterExpression? parameter = null)
	{
		parameter ??= Expression.Parameter(typeof(T), "x");
		return KeysetAdaptingExpressionVisitor.AdaptParameter(LambdaExpression, parameter);
	}

	public override IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query, KeysetPaginationDirection direction)
	{
		var lambdaExpression = MakeAccessLambdaExpression();
		var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
		return isDescending ? query.OrderByDescending(lambdaExpression) : query.OrderBy(lambdaExpression);
	}

	public override IOrderedQueryable<T> ApplyThenOrderBy(IOrderedQueryable<T> query, KeysetPaginationDirection direction)
	{
		var lambdaExpression = MakeAccessLambdaExpression();
		var isDescending = direction == KeysetPaginationDirection.Backward ? !IsDescending : IsDescending;
		return isDescending ? query.ThenByDescending(lambdaExpression) : query.ThenBy(lambdaExpression);
	}

	public override object ObtainValue<TReference>(TReference reference)
	{
		if (reference == null) throw new ArgumentNullException(nameof(reference));

		var compiledAccess = _typeToCompiledAccessMap.GetOrAdd(reference.GetType(), type =>
		{
			// Loose typing support: We'll need to adapt the lambda to the type.
			var adaptedLambdaExpression = KeysetAdaptingExpressionVisitor.AdaptType(LambdaExpression, type);
			return adaptedLambdaExpression.Compile();
		});

		// TODO: Check against null?
		var result = compiledAccess.Invoke(reference);
		return result!;
	}
}
