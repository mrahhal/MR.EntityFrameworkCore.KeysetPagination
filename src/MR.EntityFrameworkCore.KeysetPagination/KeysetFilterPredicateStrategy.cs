using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

/// <summary>
/// Represents a builder for the keyset filter predicate.
/// </summary>
internal interface IKeysetFilterPredicateStrategy
{
	Expression<Func<T, bool>> BuildKeysetFilterPredicateExpression<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		object reference);
}

internal abstract class KeysetFilterPredicateStrategy : IKeysetFilterPredicateStrategy
{
	public readonly static IKeysetFilterPredicateStrategy Default = KeysetFilterPredicateStrategyMethod1.Instance;

	private static readonly IReadOnlyDictionary<Type, MethodInfo> TypeToCompareToMethod = new Dictionary<Type, MethodInfo>
	{
		{ typeof(string), GetCompareToMethod(typeof(string)) },
		{ typeof(Guid), GetCompareToMethod(typeof(Guid)) },
		{ typeof(bool), GetCompareToMethod(typeof(bool)) },
	};
	private static readonly ConstantExpression ConstantExpression0 = Expression.Constant(0);

	public Expression<Func<T, bool>> BuildKeysetFilterPredicateExpression<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		object reference)
	{
		var referenceValues = GetValues(columns, reference);
		var referenceValueExpressions = new Expression<Func<object>>[referenceValues.Count];
		for (var i = 0; i < referenceValues.Count; i++)
		{
			var referenceValue = referenceValues[i];
			Expression<Func<object>> referenceValueExpression = () => referenceValue;
			referenceValueExpressions[i] = referenceValueExpression;
		}

		// entity =>
		var param = Expression.Parameter(typeof(T), "entity");

		var finalExpression = BuildExpressionCore(columns, direction, referenceValueExpressions, param);

		return Expression.Lambda<Func<T, bool>>(finalExpression, param);
	}

	protected abstract Expression BuildExpressionCore<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		IReadOnlyList<Expression<Func<object>>> referenceValueExpressions,
		ParameterExpression param);

	protected static List<object> GetValues<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		object reference)
	{
		var referenceValues = new List<object>(capacity: columns.Count);
		foreach (var column in columns)
		{
			var value = column.ObtainValue(reference);
			referenceValues.Add(value);
		}
		return referenceValues;
	}

	protected static BinaryExpression MakeComparisonExpression<T>(
		KeysetColumn<T> column,
		Expression memberAccess, Expression referenceValue,
		Func<Expression, Expression, BinaryExpression> compare)
	{
		if (TypeToCompareToMethod.TryGetValue(column.Type, out var compareToMethod))
		{
			// LessThan/GreaterThan operators are not valid for some types such as strings and guids.
			// We use the CompareTo method on these types instead.

			// entity.Property.CompareTo(referenceValue) >|< 0
			// -----------------------------------------

			// entity.Property.CompareTo(referenceValue)
			var methodCallExpression = Expression.Call(
				memberAccess,
				compareToMethod,
				EnsureMatchingType(memberAccess, referenceValue));

			// >|< 0
			return compare(methodCallExpression, ConstantExpression0);
		}
		else
		{
			return compare(
				EnsureAdditionalConversions(memberAccess),
				EnsureAdditionalConversions(EnsureMatchingType(memberAccess, referenceValue)));
		}
	}

	private static Expression EnsureAdditionalConversions(Expression expression)
	{
		if (expression.Type.IsEnum)
		{
			var enumUnderlyingType = Enum.GetUnderlyingType(expression.Type);

			return Expression.Convert(expression, enumUnderlyingType);
		}

		return expression;
	}

	protected static Expression EnsureMatchingType(
		Expression memberExpression,
		Expression targetExpression)
	{
		// If the target has a different type we should convert it.
		// Originally this happened with nullables only, but now that we use expressions
		// for the target access instead of constants we'll need this or else comparison won't work
		// between unmatching types (i.e int (member) compared to object (target)).
		if (memberExpression.Type != targetExpression.Type)
		{
			return Expression.Convert(targetExpression, memberExpression.Type);
		}

		return targetExpression;
	}

	protected static Func<Expression, Expression, BinaryExpression> GetComparisonExpressionToApply<T>(
		KeysetPaginationDirection direction, KeysetColumn<T> column, bool orEqual)
	{
		var greaterThan = direction switch
		{
			KeysetPaginationDirection.Forward when !column.IsDescending => true,
			KeysetPaginationDirection.Forward when column.IsDescending => false,
			KeysetPaginationDirection.Backward when !column.IsDescending => false,
			KeysetPaginationDirection.Backward when column.IsDescending => true,
			_ => throw new NotImplementedException(),
		};

		return orEqual ?
			(greaterThan ? Expression.GreaterThanOrEqual : Expression.LessThanOrEqual) :
			(greaterThan ? Expression.GreaterThan : Expression.LessThan);
	}

	protected static MethodInfo GetCompareToMethod(Type type)
	{
		var methodInfo = type.GetTypeInfo().GetMethod(nameof(string.CompareTo), new[] { type });
#pragma warning disable IDE0270 // Use coalesce expression
		if (methodInfo == null)
		{
			throw new InvalidOperationException($"Didn't find a CompareTo method on type {type.Name}.");
		}
#pragma warning restore IDE0270

		return methodInfo;
	}
}

internal class KeysetFilterPredicateStrategyMethod1 : KeysetFilterPredicateStrategy
{
	public static readonly KeysetFilterPredicateStrategyMethod1 Instance = new();

	public static bool EnableFirstColPredicateOpt = true;

	private KeysetFilterPredicateStrategyMethod1()
	{
	}

	protected override Expression BuildExpressionCore<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		IReadOnlyList<Expression<Func<object>>> referenceValueExpressions,
		ParameterExpression param)
	{
		// A composite keyset pagination in sql looks something like this:
		//   (x, y, ...) > (a, b, ...)
		// Where, x/y/... represent the column and a/b/... represent the reference's respective values.
		//
		// In sql standard this syntax is called "row value". Check here: https://use-the-index-luke.com/sql/partial-results/fetch-next-page#sb-row-values
		// Unfortunately, not all databases support this properly.
		// Further, if we were to use this we would somehow need EF Core to recognise it and translate it
		// perhaps by using a new DbFunction (https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbfunctions).
		// There's an ongoing issue for this here: https://github.com/dotnet/efcore/issues/26822
		//
		// In addition, row value won't work for mixed ordered columns. i.e if x > a but y < b.
		// So even if we can use it we'll still have to fallback to this logic in these cases.
		//
		// The generalized expression in pseudocode is:
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

		var firstMemberAccessExpression = default(Expression);
		var firstReferenceValueExpression = default(Expression);

		Expression finalExpression;

		var orExpression = default(BinaryExpression)!;
		var innerLimit = 1;
		// This loop compounds the outer OR expressions.
		for (var i = 0; i < columns.Count; i++)
		{
			var andExpression = default(BinaryExpression)!;

			// This loop compounds the inner AND expressions.
			// innerLimit implicitly grows from 1 to items.Count by each iteration.
			for (var j = 0; j < innerLimit; j++)
			{
				var isInnerLastOperation = j + 1 == innerLimit;
				var column = columns[j];
				var memberAccess = column.MakeAccessExpression(param);
				var referenceValueExpression = referenceValueExpressions[j].Body;

				if (firstMemberAccessExpression == null)
				{
					// This might be used later on in an optimization.
					firstMemberAccessExpression = memberAccess;
					firstReferenceValueExpression = referenceValueExpression;
				}

				BinaryExpression innerExpression;
				if (!isInnerLastOperation)
				{
					innerExpression = Expression.Equal(
						memberAccess,
						EnsureMatchingType(memberAccess, referenceValueExpression));
				}
				else
				{
					var compare = GetComparisonExpressionToApply(direction, column, orEqual: false);
					innerExpression = MakeComparisonExpression(
						column,
						memberAccess, referenceValueExpression,
						compare);
				}

				andExpression = andExpression == null ? innerExpression : Expression.And(andExpression, innerExpression);
			}

			orExpression = orExpression == null ? andExpression : Expression.Or(orExpression, andExpression);

			innerLimit++;
		}

		finalExpression = orExpression;

		if (EnableFirstColPredicateOpt && columns.Count > 1)
		{
			// Implement the optimization that allows an access predicate on the 1st column.
			// This is done by generating the following expression:
			//   (x >=|<= a) AND (previous generated expression)
			//
			// This effectively adds a redundant clause on the 1st column, but it's a clause all dbs
			// understand and can use as an access predicate (most commonly when the column is indexed).

			var firstColumn = columns[0];
			var compare = GetComparisonExpressionToApply(direction, firstColumn, orEqual: true);
			var accessPredicateClause = MakeComparisonExpression(
				firstColumn,
				firstMemberAccessExpression!, firstReferenceValueExpression!,
				compare);
			finalExpression = Expression.And(accessPredicateClause, finalExpression);
		}

		return finalExpression;
	}
}

internal class KeysetFilterPredicateStrategyMethod2 : KeysetFilterPredicateStrategy
{
	public static readonly KeysetFilterPredicateStrategyMethod2 Instance = new();

	private KeysetFilterPredicateStrategyMethod2()
	{
	}

	protected override Expression BuildExpressionCore<T>(
		IReadOnlyList<KeysetColumn<T>> columns,
		KeysetPaginationDirection direction,
		IReadOnlyList<Expression<Func<object>>> referenceValueExpressions,
		ParameterExpression param)
	{
		// Similar to method 1, but supposedly leads to a more performant query in the database.
		// Reference: https://use-the-index-luke.com/sql/partial-results/fetch-next-page
		//
		// But in practice, benchmarking didn't show a clear winner between the two methods in terms of performance,
		// so this method isn't used for now but remains here as a reference.
		//
		// In addition, this method's expression is a bit harder to generate and a bit harder for a human to understand.
		//
		// The generalized expression in pseudocode is:
		//   (x >= a) AND
		//   NOT (x = a AND y < b) AND
		//   NOT (x = a AND y = b AND z <= c)
		//
		// Note that the condition for when there's an equality in the comparison is a bit asymmetric.
		//
		// Simpifying the above a bit:
		//   (x >= a) AND
		//   (x != a OR y >= b) AND
		//   (x != a OR y != b OR z > c)
		//
		// In this form, equality in the comparison is applicable for all but the last outer iteration.

		Expression finalExpression;

		var andExpression = default(BinaryExpression)!;
		var innerLimit = 1;
		// This loop compounds the outer AND expressions.
		for (var i = 0; i < columns.Count; i++)
		{
			var isOuterLastOperation = i + 1 == columns.Count;
			var orExpression = default(BinaryExpression)!;

			// This loop compounds the inner OR expressions.
			// innerLimit implicitly grows from 1 to items.Count by each iteration.
			for (var j = 0; j < innerLimit; j++)
			{
				var isInnerLastOperation = j + 1 == innerLimit;
				var column = columns[j];
				var memberAccess = column.MakeAccessExpression(param);
				var referenceValueExpression = referenceValueExpressions[j].Body;

				BinaryExpression innerExpression;
				if (!isInnerLastOperation)
				{
					innerExpression = Expression.NotEqual(
						memberAccess,
						EnsureMatchingType(memberAccess, referenceValueExpression));
				}
				else
				{
					var compare = GetComparisonExpressionToApply(direction, column, orEqual: !isOuterLastOperation);
					innerExpression = MakeComparisonExpression(
						column,
						memberAccess, referenceValueExpression,
						compare);
				}

				orExpression = orExpression == null ? innerExpression : Expression.Or(orExpression, innerExpression);
			}

			andExpression = andExpression == null ? orExpression : Expression.And(andExpression, orExpression);

			innerLimit++;
		}

		finalExpression = andExpression;

		return finalExpression;
	}
}
