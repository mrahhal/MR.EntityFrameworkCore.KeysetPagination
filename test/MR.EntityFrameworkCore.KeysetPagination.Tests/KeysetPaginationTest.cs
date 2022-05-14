﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.TestModels;
using Xunit;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationTest : IClassFixture<DatabaseFixture>
{
	public KeysetPaginationTest(DatabaseFixture fixture)
	{
		var provider = fixture.BuildServices();
		DbContext = provider.GetService<TestDbContext>();
	}

	public TestDbContext DbContext { get; }

	[Fact]
	public async Task KeysetPaginate_Raw()
	{
		var result = await DbContext.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id))
			.Take(20)
			.ToListAsync();

		Assert.Equal(20, result.Count);
	}

	[Fact]
	public async Task KeysetPaginate_bool()
	{
		var reference = DbContext.IntModels.First();

		var result = await DbContext.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.IsDone),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_int()
	{
		var reference = DbContext.IntModels.First();

		var result = await DbContext.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_string()
	{
		var reference = DbContext.StringModels.First();

		var result = await DbContext.StringModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Guid()
	{
		var reference = DbContext.GuidModels.First();

		var result = await DbContext.GuidModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_int()
	{
		var reference = DbContext.IntModels.First();

		var result = await DbContext.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_string()
	{
		var reference = DbContext.StringModels.First();

		var result = await DbContext.StringModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_Guid()
	{
		var reference = DbContext.GuidModels.First();

		var result = await DbContext.GuidModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_int()
	{
		var reference = DbContext.IntModels.First();

		var result = await DbContext.IntModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_string()
	{
		var reference = DbContext.StringModels.First();

		var result = await DbContext.StringModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Guid()
	{
		var reference = DbContext.GuidModels.First();

		var result = await DbContext.GuidModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Nullable()
	{
		var reference = DbContext.NullableModels.First();

		var result = await DbContext.NullableModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Nullable2()
	{
		var reference = DbContext.NullableModels.First();

		var result = await DbContext.NullableModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Created).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_int()
	{
		var reference = DbContext.IntModels.First();

		var result = await DbContext.IntModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_string()
	{
		var reference = DbContext.StringModels.First();

		var result = await DbContext.StringModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_Guid()
	{
		var reference = DbContext.GuidModels.First();

		var result = await DbContext.GuidModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_Nullable()
	{
		var reference = DbContext.NullableModels.First();

		var result = await DbContext.NullableModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Id).Ascending(x => x.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_Nullable2()
	{
		var reference = DbContext.NullableModels.First();

		var result = await DbContext.NullableModels.KeysetPaginateQuery(
			b => b.Descending(x => x.Created).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_Nullable3()
	{
		var reference = DbContext.NullableModels.First();

		var result = await DbContext.NullableModels.KeysetPaginateQuery(
			b => b.Descending(x => x.AnotherId).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Composite_Mixed_Nullable4()
	{
		var reference = DbContext.NullableModels.First();

		var result = await DbContext.NullableModels.KeysetPaginateQuery(
			b => b.Descending(x => x.AnotherId).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeFirstReference_Empty()
	{
		var reference = DbContext.IntModels.First();

		var result = await DbContext.IntModels.KeysetPaginateQuery(
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
		var keysetContext = DbContext.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.False(result);
	}

	[Fact]
	public async Task HasPreviousAsync_True()
	{
		var reference = DbContext.IntModels.Skip(1).First();

		var keysetContext = DbContext.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference);
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.True(result);
	}

	[Fact]
	public async Task HasPreviousAsync_Incompatible()
	{
		var keysetContext = DbContext.IntModels.KeysetPaginate(
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
	public async Task HasPreviousAsync_Nullable()
	{
		var keysetContext = DbContext.NullableModels.KeysetPaginate(
			b => b.Descending(x => x.Created).Ascending(x => x.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.False(result);
	}

	[Fact]
	public async Task HasPreviousAsync_Nullable2()
	{
		var keysetContext = DbContext.NullableModels.KeysetPaginate(
			b => b.Ascending(x => x.Id).Descending(x => x.Created));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.False(result);
	}

	[Fact]
	public async Task HasPreviousAsync_Nullable3()
	{
		var keysetContext = DbContext.NullableModels.KeysetPaginate(
			b => b.Descending(x => x.AnotherId).Ascending(x => x.Id));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.False(result);
	}

	[Fact]
	public async Task HasPreviousAsync_Nullable4()
	{
		var keysetContext = DbContext.NullableModels.KeysetPaginate(
			b => b.Ascending(x => x.Id).Descending(x => x.AnotherId));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var result = await keysetContext.HasPreviousAsync(items);
		Assert.False(result);
	}

	[Fact]
	public async Task EnsureCorrectOrder_Forward()
	{
		var keysetContext = DbContext.IntModels.KeysetPaginate(
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
		var keysetContext = DbContext.IntModels.KeysetPaginate(
			b => b.Ascending(x => x.Id),
			KeysetPaginationDirection.Backward);
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();

		keysetContext.EnsureCorrectOrder(items);

		Assert.True(items[1].Id > items[0].Id, "Wrong order of ids.");
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
