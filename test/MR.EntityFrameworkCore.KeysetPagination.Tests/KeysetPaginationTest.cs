using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.TestModels;
using Xunit;

namespace MR.EntityFrameworkCore.KeysetPagination;

#pragma warning disable CA1825 // Avoid zero-length array allocations

public abstract class KeysetPaginationTest
{
	public enum QueryType
	{
		Int,
		String,
		Guid,
		Bool,
		Created,
		CreatedDesc,
		Nested,
		CreatedDescId,
		IntCreated,
	}

	private const int Size = 10;

	public KeysetPaginationTest(DatabaseFixture fixture)
	{
		var provider = fixture.BuildServices();
		DbContext = provider.GetService<TestDbContext>();
	}

	public TestDbContext DbContext { get; }

	public static IEnumerable<object[]> Queries =>
		Enum.GetValues<QueryType>()
			.Select(value => new object[] { value });

	private (Func<IQueryable<MainModel>, IQueryable<MainModel>> offsetOrderer, Action<KeysetPaginationBuilder<MainModel>> keysetPaginationBuilder) GetForQuery(QueryType queryType)
	{
		Func<IQueryable<MainModel>, IQueryable<MainModel>> offsetOrderer = queryType switch
		{
			QueryType.Int => q => q.OrderBy(x => x.Id),
			QueryType.String => q => q.OrderBy(x => x.String),
			QueryType.Guid => q => q.OrderBy(x => x.Guid),
			QueryType.Bool => q => q.OrderBy(x => x.IsDone).ThenBy(x => x.Id),
			QueryType.Created => q => q.OrderBy(x => x.Created),
			QueryType.CreatedDesc => q => q.OrderByDescending(x => x.Created),
			QueryType.Nested => q => q.OrderBy(x => x.Inner.Created),
			QueryType.IntCreated => q => q.OrderBy(x => x.Id).ThenBy(x => x.Created),
			QueryType.CreatedDescId => q => q.OrderByDescending(x => x.Created).ThenBy(x => x.Id),
			_ => throw new NotImplementedException(),
		};
		Action<KeysetPaginationBuilder<MainModel>> keysetPaginationBuilder = queryType switch
		{
			QueryType.Int => b => b.Ascending(x => x.Id),
			QueryType.String => b => b.Ascending(x => x.String),
			QueryType.Guid => q => q.Ascending(x => x.Guid),
			QueryType.Bool => q => q.Ascending(x => x.IsDone).Ascending(x => x.Id),
			QueryType.Created => b => b.Ascending(x => x.Created),
			QueryType.CreatedDesc => b => b.Descending(x => x.Created),
			QueryType.Nested => q => q.Ascending(x => x.Inner.Created),
			QueryType.IntCreated => q => q.Ascending(x => x.Id).Ascending(x => x.Created),
			QueryType.CreatedDescId => q => q.Descending(x => x.Created).Ascending(x => x.Id),
			_ => throw new NotImplementedException(),
		};

		return (offsetOrderer, keysetPaginationBuilder);
	}

	[Theory]
	[MemberData(nameof(Queries))]
	public async Task KeysetPaginate_Basic(QueryType queryType)
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(queryType);

		var expectedResult = await offsetOrderer(DbContext.MainModels)
			.Take(Size)
			.ToListAsync();

		var result = await DbContext.MainModels.KeysetPaginateQuery(
			keysetBuilder)
			.Take(Size)
			.ToListAsync();

		AssertResult(expectedResult, result);
	}

	[Theory]
	[MemberData(nameof(Queries))]
	public async Task KeysetPaginate_AfterReference(QueryType queryType)
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(queryType);

		var reference = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.Skip(Size)
			.FirstAsync();
		var expectedResult = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.Skip(Size + 1)
			.Take(Size)
			.ToListAsync();

		var result = await DbContext.MainModels.KeysetPaginateQuery(
			keysetBuilder,
			KeysetPaginationDirection.Forward,
			reference)
			.Include(x => x.Inner)
			.Take(Size)
			.ToListAsync();

		AssertResult(expectedResult, result);
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Nested_DtoReference()
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(QueryType.Nested);

		var reference = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.Skip(Size)
			.FirstAsync();
		var referenceDto = new
		{
			Inner = new
			{
				reference.Inner.Created,
			},
		};
		var expectedResult = await offsetOrderer(DbContext.MainModels)
			.Skip(Size + 1)
			.Take(Size)
			.ToListAsync();

		var result = await DbContext.MainModels.KeysetPaginateQuery(
			keysetBuilder,
			KeysetPaginationDirection.Forward,
			referenceDto)
			.Take(Size)
			.ToListAsync();

		AssertResult(expectedResult, result);
	}

	[Theory]
	[MemberData(nameof(Queries))]
	public async Task KeysetPaginate_BeforeReference(QueryType queryType)
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(queryType);

		var reference = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.Skip(Size)
			.FirstAsync();
		var expectedResult = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.Take(Size)
			.ToListAsync();

		var result = await DbContext.MainModels.KeysetPaginateQuery(
			keysetBuilder,
			KeysetPaginationDirection.Backward,
			reference)
			.Include(x => x.Inner)
			.Take(Size)
			.ToListAsync();

		AssertResult(expectedResult, result);
	}

	[Theory]
	[MemberData(nameof(Queries))]
	public async Task KeysetPaginate_BeforeFirstReference_Empty(QueryType queryType)
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(queryType);

		var reference = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.FirstAsync();

		var result = await DbContext.MainModels.KeysetPaginateQuery(
			keysetBuilder,
			KeysetPaginationDirection.Backward,
			reference)
			.Take(Size)
			.ToListAsync();

		result.Should().BeEmpty();
	}

	[Theory]
	[MemberData(nameof(Queries))]
	public async Task HasPreviousAsync_False(QueryType queryType)
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(queryType);

		var keysetContext = DbContext.MainModels.KeysetPaginate(
			keysetBuilder);
		var items = await keysetContext.Query
			.Include(x => x.Inner)
			.Take(Size)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);

		result.Should().BeFalse();
	}

	[Theory]
	[MemberData(nameof(Queries))]
	public async Task HasPreviousAsync_True(QueryType queryType)
	{
		var (offsetOrderer, keysetBuilder) = GetForQuery(queryType);

		var reference = await offsetOrderer(DbContext.MainModels)
			.Include(x => x.Inner)
			.Skip(1)
			.FirstAsync();

		var keysetContext = DbContext.MainModels.KeysetPaginate(
			keysetBuilder,
			KeysetPaginationDirection.Forward,
			reference);
		var items = await keysetContext.Query
			.Include(x => x.Inner)
			.Take(Size)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);

		result.Should().BeTrue();
	}

	[Fact]
	public async Task HasPreviousAsync_Incompatible()
	{
		var keysetContext = DbContext.MainModels.KeysetPaginate(
			b => b.Ascending(x => x.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		// A type that doesn't have an Id property which is included in the keyset above.
		var dtos = items.Select(x => new { x.Created }).ToList();

		await Assert.ThrowsAsync<KeysetPaginationIncompatibleObjectException>(async () =>
		{
			await keysetContext.HasPreviousAsync(dtos);
		});
	}

	[Fact]
	public async Task HasPreviousAsync_Incompatible_Nested_ChainPartNull()
	{
		var keysetContext = DbContext.MainModels.KeysetPaginate(
			b => b.Ascending(x => x.Inner.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		// Emulate not loading the chain.
		var dtos = items.Select(x => new { Inner = (object)null }).ToList();

		await Assert.ThrowsAsync<KeysetPaginationIncompatibleObjectException>(async () =>
		{
			await keysetContext.HasPreviousAsync(dtos);
		});
	}

	[Fact]
	public async Task HasPreviousAsync_Null_DoesNotThrow()
	{
		var keysetContext = DbContext.MainModels.KeysetPaginate(
			// Analyzer would have detected this, but assuming we suppressed the error...
			b => b.Ascending(x => x.CreatedNullable));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var dtos = items.Select(x => new { CreatedNullable = (DateTime?)null }).ToList();

		// Shouldn't throw if the user suppressed the analyzer error and knows what they're doing.
		await keysetContext.HasPreviousAsync(dtos);
	}

	[Fact]
	public async Task EnsureCorrectOrder_Forward()
	{
		var keysetContext = DbContext.MainModels.KeysetPaginate(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward);
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		keysetContext.EnsureCorrectOrder(items);

		Assert.True(items[1].Id > items[0].Id, "Wrong order of ids.");
	}

	[Fact]
	public async Task EnsureCorrectOrder_Backward()
	{
		var keysetContext = DbContext.MainModels.KeysetPaginate(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward);
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		keysetContext.EnsureCorrectOrder(items);

		Assert.True(items[1].Id > items[0].Id, "Wrong order of ids.");
	}

	[Fact]
	public async Task KeysetPaginate_DbComputed()
	{
		var reference = DbContext.MainModels.OrderBy(x => x.Id).First();

		var result = await DbContext.MainModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.CreatedComputed).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(Size)
			.ToListAsync();

		result.Should().NotBeEmpty();
	}

	[Fact]
	public async Task HasNext_DbComputed()
	{
		// The last page.
		var keysetContext = DbContext.MainModels.KeysetPaginate(
			b => b.Ascending(x => x.CreatedComputed).Ascending(x => x.Id),
			KeysetPaginationDirection.Backward);
		var data = await keysetContext.Query
			.Take(1)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(data);

		// Next on the last page => should be false
		var hasNext = await keysetContext.HasNextAsync(data);

		hasNext.Should().BeFalse();
	}

	private static void AssertResult(List<MainModel> expectedResult, List<MainModel> result)
	{
		result.Should().HaveCount(expectedResult.Count);
		result.Select(x => x.Id).Should().BeEquivalentTo(expectedResult.Select(x => x.Id));
	}
}

[Collection(SqlServerDatabaseCollection.Name)]
public class SqlServerKeysetPaginationTest : KeysetPaginationTest
{
	public SqlServerKeysetPaginationTest(SqlServerDatabaseFixture fixture)
		: base(fixture)
	{
	}
}

[Collection(SqliteDatabaseCollection.Name)]
public class SqliteKeysetPaginationTest : KeysetPaginationTest
{
	public SqliteKeysetPaginationTest(SqliteDatabaseFixture fixture)
		: base(fixture)
	{
	}
}
