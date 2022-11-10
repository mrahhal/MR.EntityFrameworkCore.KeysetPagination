using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MR.EntityFrameworkCore.KeysetPagination.Columns
{
	internal class KeysetColumnSimple<TEntity> : KeysetColumn<TEntity>, IKeysetColumn<TEntity>
	where TEntity : class
	{
		public KeysetColumnSimple(
			PropertyInfo property,
			bool isDescending)
		: base(isDescending)
		{
			Property = property;
		}

		protected PropertyInfo Property { get; }

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
	}
}
