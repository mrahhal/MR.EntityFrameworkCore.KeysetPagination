using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MR.EntityFrameworkCore.KeysetPagination.Columns
{
	internal class KeysetColumnNested<TEntity> : KeysetColumn<TEntity>, IKeysetColumn<TEntity>
	where TEntity : class
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
		protected PropertyInfo Property { get; }


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
	}
}
