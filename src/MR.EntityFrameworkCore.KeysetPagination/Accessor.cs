using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination
{
	internal class Accessor
	{
		private static readonly Hashtable Lookup = new Hashtable();

		private readonly Hashtable _propertyInfoLookup = new Hashtable();

		public IReadOnlyList<string> Keys { get; private set; }

		public bool HasProperty(string key)
		{
			return _propertyInfoLookup.ContainsKey(key);
		}

		public object GetPropertyValue(object instance, string key)
		{
			var propertyInfo = _propertyInfoLookup[key] as PropertyInfo;
			return propertyInfo.GetMethod.Invoke(instance, Array.Empty<object>());
		}

		public void SetPropertyValue(object instance, string key, object value)
		{
			var propertyInfo = _propertyInfoLookup[key] as PropertyInfo;
			propertyInfo.SetMethod.Invoke(instance, new object[] { value });
		}

		public static Accessor Obtain<T>()
		{
			return Obtain(typeof(T));
		}

		public static Accessor Obtain(Type type)
		{
			var accessor = (Accessor)Lookup[type];
			if (accessor != null) return accessor;

			lock (Lookup)
			{
				// double-check
				accessor = (Accessor)Lookup[type];
				if (accessor != null) return accessor;

				accessor = CreateNew(type);

				Lookup[type] = accessor;
				return accessor;
			}
		}

		private static Accessor CreateNew(Type type)
		{
			var accessor = new Accessor();

			var properties = type.GetProperties();
			accessor.Keys = properties.Select(x => x.Name).ToList().AsReadOnly();

			foreach (var p in properties)
			{
				accessor._propertyInfoLookup[p.Name] = p;
			}

			return accessor;
		}
	}
}
