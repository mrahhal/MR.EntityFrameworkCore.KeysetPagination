using System.Collections.Concurrent;
using System.Reflection;

namespace MR.EntityFrameworkCore.KeysetPagination;

internal class Accessor
{
	private static readonly ConcurrentDictionary<Type, Accessor> TypeToAccessorMap = new();

	private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyInfoMap;

	public Accessor(
		IReadOnlyDictionary<string, PropertyInfo> propertyInfoLookup)
	{
		_propertyInfoMap = propertyInfoLookup;
	}

	public bool HasProperty(string key)
	{
		return _propertyInfoMap.ContainsKey(key);
	}

	public bool TryGetPropertyValue(object instance, string key, out object? value)
	{
		value = null;
		if (!_propertyInfoMap.TryGetValue(key, out var propertyInfo))
		{
			return false;
		}
		value = propertyInfo.GetMethod!.Invoke(instance, Array.Empty<object>())!;
		return true;
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

		var propertyInfoLookup = new Dictionary<string, PropertyInfo>(capacity: properties.Length);
		foreach (var p in properties)
		{
			propertyInfoLookup[p.Name] = p;
		}

		return new(propertyInfoLookup);
	}
}
