using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
	public async Task KeysetPaginate_AfterReference_Enum()
	{
		var reference = DbContext.EnumModels.First();

		var result = await DbContext.EnumModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.EnumType).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Nested()
	{
		var reference = DbContext.NestedModels.Include(x => x.Inner).First();

		var result = await DbContext.NestedModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_Nested_DtoReference()
	{
		var reference = DbContext.NestedModels.Include(x => x.Inner).First();
		var referenceDto = new
		{
			Inner = new
			{
				reference.Inner.Created,
			},
		};

		var result = await DbContext.NestedModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Created),
			KeysetPaginationDirection.Forward,
			referenceDto)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_Int()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Data.RootElement.GetProperty("nbInt").GetInt32()),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_String()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Data.RootElement.GetProperty("nbString").GetString()),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_Int_FromString()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending("inner.data.nbInt",
							typeof(JsonElement).GetMethod(nameof(JsonElement.GetInt32),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null)),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_String_FromString()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending("inner.data.nbString",
							typeof(JsonElement).GetMethod(nameof(JsonElement.GetString),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null)),
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
	public async Task KeysetPaginate_BeforeReference_Enum()
	{
		var reference = DbContext.EnumModels.First();

		var result = await DbContext.EnumModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.EnumType).Ascending(x => x.Id),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}
	[Fact]
	public async Task KeysetPaginate_BeforeReference_NestedJson_Int()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Data.RootElement.GetProperty("nbInt").GetInt32()),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_NestedJson_String()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Data.RootElement.GetProperty("nbString").GetString()),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_NestedJson_Int_FromString()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending("inner.data.nbInt",
							typeof(JsonElement).GetMethod(nameof(JsonElement.GetInt32),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null)),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_BeforeReference_NestedJson_String_FromString()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending("inner.data.nbString",
							typeof(JsonElement).GetMethod(nameof(JsonElement.GetString),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null)),
			KeysetPaginationDirection.Backward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_Int_Composite()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Data.RootElement.GetProperty("nbInt").GetInt32())
					.Descending(x => x.Inner.Created),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_String_Composite()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending(x => x.Inner.Data.RootElement.GetProperty("nbString").GetString())
					.Descending("inner.created"),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_Int_FromString_Composite()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending("inner.data.nbInt",
							typeof(JsonElement).GetMethod(nameof(JsonElement.GetInt32),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null))
					.Descending("inner.created"),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();
	}

	[Fact]
	public async Task KeysetPaginate_AfterReference_NestedJson_String_FromString_Composite()
	{
		if (!DatabaseFixture.UsePostgresqlServer)
		{
			return;
		}

		var reference = DbContext.Set<NestedJsonModel>().Include(x => x.Inner).First();

		var result = await DbContext.Set<NestedJsonModel>().KeysetPaginateQuery(
			b => b.Ascending("inner.data.nbString",
							typeof(JsonElement).GetMethod(nameof(JsonElement.GetString),
								bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
								null,
								new Type[] { }, null))
					.Descending(x => x.Inner.Created),
			KeysetPaginationDirection.Forward,
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
	public async Task HasPreviousAsync_Incompatible_Nested_ChainPartNull()
	{
		var keysetContext = DbContext.NestedModels.KeysetPaginate(
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
		var keysetContext = DbContext.ComputedModels.KeysetPaginate(
			// Analyzer would have detected this, but assuming we suppressed the error...
			b => b.Ascending(x => x.Created));
		var items = await keysetContext.Query
			.Take(20)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(items);

		var dtos = items.Select(x => new { Created = (DateTime?)null }).ToList();

		// Shouldn't throw if the user suppressed the analyzer error and knows what they're doing.
		await keysetContext.HasPreviousAsync(dtos);
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

	[Fact]
	public async Task KeysetPaginate_DbComputed()
	{
		var reference = DbContext.ComputedModels.OrderBy(x => x.Id).First();

		var result = await DbContext.ComputedModels.KeysetPaginateQuery(
			b => b.Ascending(x => x.CreatedComputed).Ascending(x => x.Id),
			KeysetPaginationDirection.Forward,
			reference)
			.Take(20)
			.ToListAsync();

		Assert.True(result.Any());
	}

	[Fact]
	public async Task HasNext_DbComputed()
	{
		// The last page
		var keysetContext = DbContext.ComputedModels.KeysetPaginate(
			b => b.Ascending(x => x.CreatedComputed).Ascending(x => x.Id),
			KeysetPaginationDirection.Backward);
		var data = await keysetContext.Query
			.Take(1)
			.ToListAsync();
		keysetContext.EnsureCorrectOrder(data);

		// Next on the last page => should be false
		var hasNext = await keysetContext.HasNextAsync(data);

		Assert.False(hasNext);
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
