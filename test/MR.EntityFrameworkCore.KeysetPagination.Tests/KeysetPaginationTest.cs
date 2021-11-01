using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lapis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.Tests.Models;
using Xunit;

namespace MR.EntityFrameworkCore.KeysetPagination.Tests
{
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
		public async Task KeysetPaginate_AfterReference()
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
		public async Task KeysetPaginate_AfterReference2()
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
		public async Task KeysetPaginate_AfterReference3()
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
		public async Task KeysetPaginate_BeforeReference()
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
		public async Task KeysetPaginate_BeforeReference2()
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
		public async Task KeysetPaginate_BeforeReference3()
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
		public async Task KeysetPaginate_AfterReference_Composite()
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
		public async Task KeysetPaginate_AfterReference_Composite2()
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
		public async Task KeysetPaginate_AfterReference_Composite3()
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
		public async Task KeysetPaginate_AfterReference_Composite_Mixed()
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
		public async Task KeysetPaginate_AfterReference_Composite_Mixed2()
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
		public async Task KeysetPaginate_AfterReference_Composite_Mixed3()
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

		public override void Dispose()
		{
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
}
