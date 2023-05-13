namespace MR.EntityFrameworkCore.KeysetPagination;

public static class KeysetQuery
{
	/// <summary>
	/// Builds a keyset query definition.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <param name="builderAction">An action that takes a builder and registers the columns upon which keyset pagination will work.</param>
	/// <returns>The <see cref="KeysetQueryDefinition{T}"/> representing the built keyset query definition.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="builderAction"/> is null.</exception>
	public static KeysetQueryDefinition<T> Build<T>(
		Action<KeysetPaginationBuilder<T>> builderAction)
	{
		if (builderAction == null) throw new ArgumentNullException(nameof(builderAction));

		var columns = BuildColumns(builderAction);
		return new KeysetQueryDefinition<T>(columns);
	}

	internal static IReadOnlyList<KeysetColumn<T>> BuildColumns<T>(
		Action<KeysetPaginationBuilder<T>> builderAction)
	{
		var builder = new KeysetPaginationBuilder<T>();
		builderAction(builder);

		return builder.Columns;
	}
}
