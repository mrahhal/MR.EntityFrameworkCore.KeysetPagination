using System.Collections.Concurrent;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal class Accessor
{
	private static readonly ConcurrentDictionary<Type, Accessor> TypeToAccessorMap = new();

	private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyInfoMap;

	public Accessor(
		IReadOnlyList<string> keys,
		IReadOnlyDictionary<string, PropertyInfo> propertyInfoLookup)
	{
		Keys = keys;
		_propertyInfoMap = propertyInfoLookup;
	}

	public IReadOnlyList<string> Keys { get; private set; }

	public bool HasProperty(string key)
	{
		return _propertyInfoMap.ContainsKey(key);
	}

	public object GetPropertyValue(object instance, string key)
	{
		var propertyInfo = _propertyInfoMap[key];
		return propertyInfo.GetMethod!.Invoke(instance, Array.Empty<object>())!;
	}

	public void SetPropertyValue(object instance, string key, object value)
	{
		var propertyInfo = _propertyInfoMap[key];
		propertyInfo.SetMethod!.Invoke(instance, new object[] { value });
	}

	public static Accessor Obtain<T>()
	{
		return Obtain(typeof(T));
	}

	public static Accessor Obtain(Type type)
	{
		return TypeToAccessorMap.GetOrAdd(type, type => CreateNew(type));
	}

	private static Accessor CreateNew(Type type)
	{
		var properties = type.GetProperties();
		var keys = properties.Select(x => x.Name).ToList().AsReadOnly();

		var propertyInfoLookup = new Dictionary<string, PropertyInfo>(capacity: properties.Length);
		foreach (var p in properties)
		{
			propertyInfoLookup[p.Name] = p;
		}

		return new(keys, propertyInfoLookup);
	}
}
