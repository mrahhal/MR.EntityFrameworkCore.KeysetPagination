using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.TestModels;

namespace MR.EntityFrameworkCore.KeysetPagination;

public class DatabaseFixture : IDisposable
{
	public DatabaseFixture()
	{
		SetupDatabase();
	}

	public IServiceProvider BuildServices(Action<IServiceCollection> configureServices = null)
	{
		var services = new ServiceCollection();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=test.db"));
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
		var provider = BuildServices();

		using var scope = provider.CreateScope();
		var context = scope.ServiceProvider.GetService<TestDbContext>();
		context.Database.EnsureDeleted();
		context.Database.EnsureCreated();

		FillData(context);
	}

	private void FillData(TestDbContext context)
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
