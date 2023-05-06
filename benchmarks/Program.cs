using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MR.EntityFrameworkCore.KeysetPagination;

BenchmarkRunner.Run<Benchmarks>();

[MemoryDiagnoser(displayGenColumns: false)]
[CsvMeasurementsExporter]
[Config(typeof(Config))]
public class Benchmarks
{
	private class Config : ManualConfig
	{
		public Config()
		{
			AddJob(Job.ShortRun);
		}
	}

	private ServiceProvider _provider = default!;
	private User _midPageReference = default!;
	private User _lastPageReference = default!;

	Func<IQueryable<User>, IQueryable<User>> _offsetOrderer = default!;
	Action<KeysetPaginationBuilder<User>> _keysetPaginationBuilder = default!;

	[Params(1_000, 10_000, 100_000, 1_000_000, 10_000_000)]
	public int N;

	[ParamsAllValues]
	public Orders Order;

	private BenchmarkDbContext GetDbContext() => _provider.GetRequiredService<BenchmarkDbContext>();

	[GlobalSetup]
	public void Setup()
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
	public void Cleanup()
	{
		_provider.Dispose();
	}

	[Benchmark]
	public async Task<object> Offset_FirstPage()
	{
		using var context = GetDbContext();

		var result = await _offsetOrderer(context.Users)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Keyset_FirstPage()
	{
		using var context = GetDbContext();

		var result = await context.Users.KeysetPaginateQuery(_keysetPaginationBuilder)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Offset_MidPage()
	{
		using var context = GetDbContext();

		var result = await _offsetOrderer(context.Users)
			.Skip(N / 2)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Keyset_MidPage()
	{
		using var context = GetDbContext();

		var result = await context.Users.KeysetPaginateQuery(
			_keysetPaginationBuilder,
			KeysetPaginationDirection.Forward,
			_midPageReference)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Offset_LastPage()
	{
		using var context = GetDbContext();

		var result = await _offsetOrderer(context.Users)
			.Skip(N - 20)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Keyset_LastPage()
	{
		using var context = GetDbContext();

		var result = await context.Users.KeysetPaginateQuery(
			_keysetPaginationBuilder,
			KeysetPaginationDirection.Forward,
			_lastPageReference)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Keyset_LastPage_Backward()
	{
		using var context = GetDbContext();

		var result = await context.Users.KeysetPaginateQuery(
			_keysetPaginationBuilder,
			KeysetPaginationDirection.Backward)
			.Take(20)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<object> Keyset_SecondToLastPage_Before()
	{
		using var context = GetDbContext();

		var result = await context.Users.KeysetPaginateQuery(
			_keysetPaginationBuilder,
			KeysetPaginationDirection.Backward,
			_lastPageReference)
			.Take(20)
			.ToListAsync();

		return result;
	}
}

public enum Orders
{
	Id,
	CreatedDesc,
	CreatedId,
	CreatedDescId, // Not indexed
	CreatedDescIdDesc,
}
