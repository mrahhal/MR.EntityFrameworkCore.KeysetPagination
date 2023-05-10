using Xunit;

namespace MR.EntityFrameworkCore.KeysetPagination;

[CollectionDefinition(Name)]
public class SqlServerDatabaseCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
	public const string Name = nameof(SqlServerDatabaseCollection);
}

[CollectionDefinition(Name)]
public class SqliteDatabaseCollection : ICollectionFixture<SqliteDatabaseFixture>
{
	public const string Name = nameof(SqliteDatabaseCollection);
}
