using System.Linq.Expressions;

namespace MR.EntityFrameworkCore.KeysetPagination.Columns
{
	public interface IKeysetColumn<TEntity>
	where TEntity : class
	{
		bool IsDescending { get; }
		Expression MakeMemberAccessExpression(ParameterExpression param);
		object ObtainValue(object reference);
		IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> query, KeysetPaginationDirection direction);
		IOrderedQueryable<TEntity> ApplyThenOrderBy(IOrderedQueryable<TEntity> query, KeysetPaginationDirection direction);
	}
}
