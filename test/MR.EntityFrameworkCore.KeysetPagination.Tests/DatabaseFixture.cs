using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.TestModels;

namespace MR.EntityFrameworkCore.KeysetPagination;

public abstract class DatabaseFixture : IDisposable
{
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
			ConfigureDb(options);
			options.EnableSensitiveDataLogging();
		});
		configureServices?.Invoke(services);
		return services.BuildServiceProvider();
	}

	protected abstract void ConfigureDb(DbContextOptionsBuilder options);

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
		if (_initialized) return;

		var provider = BuildServices();

		using var scope = provider.CreateScope();
		var context = scope.ServiceProvider.GetService<TestDbContext>();
		context.Database.EnsureDeleted();
		context.Database.EnsureCreated();

		Seed(context);

		_initialized = true;
	}

	private void Seed(TestDbContext context)
	{
		var now = DateTime.Now.AddYears(-1);
		var rand = new Random(3999);

		for (var i = 1; i < 100; i++)
		{
			var created = now.AddMinutes(i);
			context.MainModels.Add(new MainModel
			{
				String = i.ToString(),
				Guid = Guid.NewGuid(),
				IsDone = i % 2 == 0,
				Created = created,
				CreatedNullable = i % 2 == 0 ? created : null,
				//CreatedNormal = DateTime.Parse("9999-12-31T00:00:00.0000000"),
				Inner = new NestedInnerModel
				{
					Created = created,
				},
				Inners2 = Enumerable.Range(0, rand.Next(10)).Select(i => new NestedInner2Model()).ToList(),
				EnumValue = i % 2 == 0 ? EnumType.Value1 : EnumType.Value2,
			});
		}

		context.SaveChanges();
	}
}

public class SqlServerDatabaseFixture : DatabaseFixture
{
	protected override void ConfigureDb(DbContextOptionsBuilder options)
	{
		options.UseSqlServer(
			  "Server=(localdb)\\mssqllocaldb;Database=KeysetPaginationTest;Trusted_Connection=True;MultipleActiveResultSets=true");
	}
}

public class SqliteDatabaseFixture : DatabaseFixture
{
	protected override void ConfigureDb(DbContextOptionsBuilder options)
	{
		options.UseSqlite("Data Source=test.db");
	}
}
