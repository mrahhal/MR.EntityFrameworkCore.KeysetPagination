using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination;

public abstract class BenchmarksBase
{
	protected ServiceProvider _provider = default!;
	protected User _midPageReference = default!;
	protected User _lastPageReference = default!;

	protected Func<IQueryable<User>, IQueryable<User>> _offsetOrderer = default!;
	protected Action<KeysetPaginationBuilder<User>> _keysetPaginationBuilder = default!;

	[Params(1_000, 10_000, 100_000, 1_000_000, 10_000_000)]
	public int N;

	[ParamsAllValues]
	public Orders Order;

	protected BenchmarkDbContext GetDbContext() => _provider.GetRequiredService<BenchmarkDbContext>();

	[GlobalSetup]
	public virtual void Setup()
	{
		var services = new ServiceCollection();

		services.AddDbContext<BenchmarkDbContext>(options =>
		{
			var dbName = $"KeysetPaginationBenchmarksBasic{N}";
			options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true");
			// We're interested in raw perf without the overhead of EF tracking.
			options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
		}, ServiceLifetime.Transient);

		_provider = services.BuildServiceProvider();

		using var context = GetDbContext();
		//context.Database.EnsureDeleted();
		context.Database.EnsureCreated();
		using var connection = context.Database.GetDbConnection();
		if (!context.Users.Any())
		{
			var users = new List<User>(capacity: N);
			for (var i = 0; i < N; i++)
			{
				var created = DateTime.Now + TimeSpan.FromSeconds(i);
				users.Add(new User { Name = $"{i}", Created = created });
			}

			connection.Execute(
				"INSERT INTO Users (Name, Created) VALUES (@name, @created)",
				users);
		}

		_midPageReference = context.Users.Skip(N / 2).First();
		_lastPageReference = context.Users.Skip(N - 20).First();

		_offsetOrderer = Order switch
		{
			Orders.Id => q => q.OrderBy(x => x.Id),
			Orders.CreatedDesc => q => q.OrderByDescending(x => x.Created),
			Orders.CreatedId => q => q.OrderBy(x => x.Created).ThenBy(x => x.Id),
			Orders.CreatedDescId => q => q.OrderByDescending(x => x.Created).ThenBy(x => x.Id),
			Orders.CreatedDescIdDesc => b => b.OrderByDescending(x => x.Created).ThenByDescending(x => x.Id),
			_ => throw new NotImplementedException(),
		};
		_keysetPaginationBuilder = Order switch
		{
			Orders.Id => b => b.Ascending(x => x.Id),
			Orders.CreatedDesc => b => b.Descending(x => x.Created),
			Orders.CreatedId => b => b.Ascending(x => x.Created).Ascending(x => x.Id),
			Orders.CreatedDescId => b => b.Descending(x => x.Created).Ascending(x => x.Id),
			Orders.CreatedDescIdDesc => b => b.Descending(x => x.Created).Descending(x => x.Id),
			_ => throw new NotImplementedException(),
		};
	}

	[GlobalCleanup]
	public virtual void Cleanup()
	{
		_provider.Dispose();
	}
}
