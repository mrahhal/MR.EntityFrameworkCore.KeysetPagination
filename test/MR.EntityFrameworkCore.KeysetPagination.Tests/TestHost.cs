using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination.TestModels;

namespace MR.EntityFrameworkCore.KeysetPagination;

public abstract class TestHost : IDisposable
{
	private static bool _initialized;

	public TestHost()
	{
		var services = new ServiceCollection();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=test.db"));
		ConfigureServices(services);
		Provider = services.BuildServiceProvider();

		EnsureSetup();
	}

	public IServiceProvider Provider { get; }

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	protected virtual void ConfigureServices(IServiceCollection services)
	{
	}

	protected IServiceScope CreateScope()
	{
		return Provider.CreateScope();
	}

	private void EnsureSetup()
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

		FillData(context);
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
