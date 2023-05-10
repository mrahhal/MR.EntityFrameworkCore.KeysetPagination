using Xunit;

namespace MR.EntityFrameworkCore.KeysetPagination;

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
	public const string Name = nameof(DatabaseCollection);
}
