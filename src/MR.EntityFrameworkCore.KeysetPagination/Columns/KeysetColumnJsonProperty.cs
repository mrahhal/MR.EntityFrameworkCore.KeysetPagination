using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MR.EntityFrameworkCore.KeysetPagination.Columns
{
	internal class KeysetColumnJsonProperty<TEntity> : KeysetColumn<TEntity>, IKeysetColumn<TEntity>
	where TEntity : class
	{
		public KeysetColumnJsonProperty(
			List<PropertyInfo> properties,
			List<string> jsonProperties,
			MethodInfo? jsonElementToValue,
			bool isDescending)
		: base(isDescending)
		{
			Debug.Assert(properties.Count > 1, "Expected this to be a chain of nested properties (more than 1 property).");
			Properties = properties;
			JsonProperties = jsonProperties;
			JsonElementToValue = jsonElementToValue ?? GetDefaultJsonElementToValueConverter();
		}

		private IReadOnlyList<PropertyInfo> Properties { get; }
		private IReadOnlyList<string> JsonProperties { get; }
		private MethodInfo JsonElementToValue { get; }

		// Default GetString converter to try on JsonElement if not defined
		private static MethodInfo GetDefaultJsonElementToValueConverter()
		{
			var method = typeof(JsonElement).GetMethod(nameof(JsonElement.GetString),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null);
			if (method == null)
			{
				throw new InvalidOperationException("Cannot find default method 'GetString' on 'JsonElement' type");
			}

			return method;
		}

		public override Expression MakeMemberAccessExpression(ParameterExpression param)
		{
			// x.Nested.JsonProperty.GetProperty("zz").GetString()
			// -----------------

			var resultingExpression = default(MemberExpression);
			foreach (var prop in Properties.Take(Properties.Count - 1))
			{
				var chainAnchor = resultingExpression ?? (Expression)param;
				resultingExpression = Expression.MakeMemberAccess(chainAnchor, prop);
			}

			var jsonExpr = ExpressionHelper.AppendJsonElementsExpression(resultingExpression!, JsonProperties.ToList());

			return Expression.Call(jsonExpr, JsonElementToValue);
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

			var getPropMeth = lastValue?.GetType().GetMethod(nameof(JsonElement.GetProperty),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new[] { typeof(string) }, null);
			foreach (var jsonProp in JsonProperties)
			{
				lastValue = getPropMeth?.Invoke(lastValue, new object[] { jsonProp });
			}

			lastValue = JsonElementToValue.Invoke(lastValue, null);

			return CheckAndReturn(lastValue, lastPropertyName);
		}
	}
}
