using System;
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
		public void KeysetPaginate_ReferenceWithNoDirection_Throws()
		{
			var reference = Context.IntModels.First();

			Assert.Throws<ArgumentException>(() =>
			{
				Context.IntModels.KeysetPaginate(
					b => b.Ascending(x => x.Id),
					reference);
			});
		}

		[Fact]
		public void KeysetPaginate_DirectionWithNoReference_Throws()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				Context.IntModels.KeysetPaginate(
					b => b.Ascending(x => x.Id),
					direction: KeysetPaginationReferenceDirection.After);
			});
		}

		[Fact]
		public async Task KeysetPaginate_Raw()
		{
			var result = await Context.IntModels.KeysetPaginate(
				b => b.Ascending(x => x.Id))
				.Take(20)
				.ToListAsync();

			Assert.Equal(20, result.Count);
		}

		[Fact]
		public async Task KeysetPaginate_AfterReference()
		{
			var reference = Context.IntModels.First();

			var result = await Context.IntModels.KeysetPaginate(
				b => b.Ascending(x => x.Id),
				reference,
				KeysetPaginationReferenceDirection.After)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_AfterReference2()
		{
			var reference = Context.StringModels.First();

			var result = await Context.StringModels.KeysetPaginate(
				b => b.Ascending(x => x.Id),
				reference,
				KeysetPaginationReferenceDirection.After)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_BeforeReference()
		{
			var reference = Context.IntModels.First();

			var result = await Context.IntModels.KeysetPaginate(
				b => b.Ascending(x => x.Id),
				reference,
				KeysetPaginationReferenceDirection.Before)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_BeforeReference2()
		{
			var reference = Context.StringModels.First();

			var result = await Context.StringModels.KeysetPaginate(
				b => b.Ascending(x => x.Id),
				reference,
				KeysetPaginationReferenceDirection.Before)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_AfterReference_Composite()
		{
			var reference = Context.IntModels.First();

			var result = await Context.IntModels.KeysetPaginate(
				b => b.Ascending(x => x.Id).Ascending(x => x.Created),
				reference,
				KeysetPaginationReferenceDirection.After)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_AfterReference_Composite2()
		{
			var reference = Context.StringModels.First();

			var result = await Context.StringModels.KeysetPaginate(
				b => b.Ascending(x => x.Id).Ascending(x => x.Created),
				reference,
				KeysetPaginationReferenceDirection.After)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_AfterReference_Composite_Mixed()
		{
			var reference = Context.IntModels.First();

			var result = await Context.IntModels.KeysetPaginate(
				b => b.Descending(x => x.Id).Ascending(x => x.Created),
				reference,
				KeysetPaginationReferenceDirection.After)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_AfterReference_Composite_Mixed2()
		{
			var reference = Context.StringModels.First();

			var result = await Context.StringModels.KeysetPaginate(
				b => b.Descending(x => x.Id).Ascending(x => x.Created),
				reference,
				KeysetPaginationReferenceDirection.After)
				.Take(20)
				.ToListAsync();
		}

		[Fact]
		public async Task KeysetPaginate_BeforeFirstReference_Empty()
		{
			var reference = Context.IntModels.First();

			var result = await Context.IntModels.KeysetPaginate(
				b => b.Ascending(x => x.Id),
				reference,
				KeysetPaginationReferenceDirection.Before)
				.Take(20)
				.ToListAsync();

			Assert.Empty(result);
		}

		public override void Dispose()
		{
			_scope.Dispose();
			base.Dispose();
		}
	}
}
