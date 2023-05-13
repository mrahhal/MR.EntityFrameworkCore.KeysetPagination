namespace MR.EntityFrameworkCore.KeysetPagination;

/// <summary>
/// Represents a definition of a keyset query.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public class KeysetQueryDefinition<T>
	where T : class
{
	internal KeysetQueryDefinition(
		IReadOnlyList<KeysetColumn<T>> columns)
	{
		Columns = columns;
	}

	internal IReadOnlyList<KeysetColumn<T>> Columns { get; }
}
