using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.TestModels;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class DatabaseFixture : IDisposable
{
	private static readonly object _lock = new();
	private static bool _initialized;

	public DatabaseFixture()
	{
		SetupDatabase();
	}

	public IServiceProvider BuildServices(Action<IServiceCollection> configureServices = null)
	{
		var services = new ServiceCollection();
		services.AddDbContext<TestDbContext>(options =>
		{
			options.UseSqlite("Data Source=test.db");
			//options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=KeysetPaginationTest;Trusted_Connection=True;MultipleActiveResultSets=true");
			options.EnableSensitiveDataLogging();
		});
		configureServices?.Invoke(services);
		return services.BuildServiceProvider();
	}

	public IServiceProvider BuildForService<T>(Action<IServiceCollection> configureServices = null)
		where T : class
	{
		return BuildServices(services =>
		{
			configureServices?.Invoke(services);
			services.AddTransient<T>();
		});
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	private void SetupDatabase()
	{
		lock (_lock)
		{
			if (_initialized) return;

			var provider = BuildServices();

			using var scope = provider.CreateScope();
			var context = scope.ServiceProvider.GetService<TestDbContext>();
			context.Database.EnsureDeleted();
			context.Database.EnsureCreated();

			Seed(context);

			_initialized = true;
		}
	}

	private void Seed(TestDbContext context)
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
				Created = created,
			});
			context.GuidModels.Add(new GuidModel
			{
				Id = Guid.NewGuid(),
				Created = created,
			});
			context.NestedModels.Add(new NestedModel
			{
				Inner = new NestedInnerModel
				{
					Created = created,
				},
			});
			context.ComputedModels.Add(new ComputedModel
			{
				Created = null,
				Name = i.ToString("D5"),
			});
		}

		context.SaveChanges();
	}
}
