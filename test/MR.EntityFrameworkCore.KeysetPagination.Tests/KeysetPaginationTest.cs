using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.Tests.Models;
using Xunit;

namespace MR.EntityFrameworkCore.KeysetPagination.Tests;

public class KeysetPaginationTest : TestHost
{
	private readonly IServiceScope _scope;

	public KeysetPaginationTest()
	{
		_scope = CreateScope();
		Context = _scope.ServiceProvider.GetService<TestDbContext>();
	}

	public TestDbContext Context { get; }

	[Fact]
	public async Task KeysetPaginate_Raw()
	{
		var result = await Context.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id))
			.Take(20)
			.ToListAsync();

		Assert.Equal(20, result.Count);
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_int()
	{
		var reference = Context.IntModels.First();

		var result = await Context.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_string()
	{
		var reference = Context.StringModels.First();

		var result = await Context.StringModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Guid()
	{
		var reference = Context.GuidModels.First();

		var result = await Context.GuidModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_int()
	{
		var reference = Context.IntModels.First();

		var result = await Context.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_string()
	{
		var reference = Context.StringModels.First();

		var result = await Context.StringModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_Guid()
	{
		var reference = Context.GuidModels.First();

		var result = await Context.GuidModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_int()
	{
		var reference = Context.IntModels.First();

		var result = await Context.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_string()
	{
		var reference = Context.StringModels.First();

		var result = await Context.StringModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Guid()
	{
		var reference = Context.GuidModels.First();

		var result = await Context.GuidModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_int()
	{
		var reference = Context.IntModels.First();

		var result = await Context.IntModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_string()
	{
		var reference = Context.StringModels.First();

		var result = await Context.StringModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_Guid()
	{
		var reference = Context.GuidModels.First();

		var result = await Context.GuidModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeFirstReference_Empty()
	{
		var reference = Context.IntModels.First();

		var result = await Context.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();

		Assert.Empty(result);
	}

	[Fact]
	public async Task HasPreviousAsync_False()
	{
		var keysetContext = Context.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.False(result);
	}

	[Fact]
	public async Task HasPreviousAsync_True()
	{
		var reference = Context.IntModels.Skip(1).First();

		var keysetContext = Context.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference);
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.True(result);
	}

	[Fact]
	public async Task HasPreviousAsync_Incompatible()
	{
		var keysetContext = Context.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		// A type that doesn't have an Id property which is included in the columns definition above.
		var dtos = items.Select(x => new { x.Created }).ToList();

		await Assert.ThrowsAsync<KeysetPaginationIncompatibleObjectException>(async () =>
		{
			await keysetContext.HasPreviousAsync(dtos);
		});
	}

	[Fact]
	public async Task EnsureCorrectOrder_Forward()
	{
		var keysetContext = Context.IntModels.KeysetPaginate(
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
		var keysetContext = Context.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward);
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		keysetContext.EnsureCorrectOrder(items);

		Assert.True(items[1].Id > items[0].Id, "Wrong order of ids.");
	}

	public override void Dispose()
	{
		GC.SuppressFinalize(this);

		_scope.Dispose();
		base.Dispose();
	}

	private void AssertRange(int from, int to, List<IntModel> actual)
	{
		AssertRange(from, to, actual.Select(x => x.Id).ToList());
	}

	private void AssertRange(int from, int to, List<StringModel> actual)
	{
		AssertRange(from, to, actual.Select(x => int.Parse(x.Id)).ToList());
	}

	private void AssertRange(int from, int to, List<int> actual)
	{
		var expected = new List<int>();
		for (var i = from; i < to; i++)
		{
			expected.Add(i);
		}
		Assert.Equal(expected, actual);
	}
}
