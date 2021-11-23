using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.Tests.Models;

namespace Lapis
{
	public abstract class TestHost : IDisposable
	{
		private static bool _initialized;

		static TestHost()
		{
			var services = new ServiceCollection();
			services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=test.db"));
			Provider = services.BuildServiceProvider();
		}

		public TestHost()
		{
			EnsureSetup();
		}

		static public IServiceProvider Provider { get; }

		public virtual void Dispose()
		{
		}

		protected static IServiceScope CreateScope()
		{
			return Provider.CreateScope();
		}

		private static void EnsureSetup()
		{
			if (_initialized)
			{
				return;
			}

			_initialized = true;

			using var scope = CreateScope();
			var context = scope.ServiceProvider.GetService<TestDbContext>();
			context.Database.EnsureDeleted();
			context.Database.EnsureCreated();

			if (!context.StringModels.Any())
			{
				FillData(context);
			}
		}

		private static void FillData(TestDbContext context)
		{
			var now = DateTime.Now.AddYears(-1);

			for (var i = 1; i < 1001; i++)
			{
				var created = now.AddMinutes(i);
				context.StringModels.Add(new StringModel
				{
					Id = i.ToString(),
					Created = created,
				});
				context.IntModels.Add(new IntModel
				{
					Id = i,
					Created = created,
				});
				context.GuidModels.Add(new GuidModel
				{
					Id = Guid.NewGuid(),
					Created = created,
				});
			}

			context.SaveChanges();
		}
	}
}
