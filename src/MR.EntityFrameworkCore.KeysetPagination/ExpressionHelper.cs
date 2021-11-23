using System.Linq.Expressions;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal static class ExpressionHelper
{
	public static PropertyInfo GetPropertyInfoFromMemberAccess<T, TProp>(
		Expression<Func<T, TProp>> expression)
	{
		MemberExpression memberExpression;
		if (expression.Body.NodeType == ExpressionType.Convert)
		{
			memberExpression = (MemberExpression)((UnaryExpression)expression.Body).Operand;
		}
		else
		{
			memberExpression = (MemberExpression)expression.Body;
		}

		var propertyInfo = (PropertyInfo)memberExpression.Member;
		return propertyInfo;
	}
}
