using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

[CsvMeasurementsExporter]
[MemoryDiagnoser(displayGenColumns: false)]
[Config(typeof(Config))]
public class FirstColPredicateOptBenchmarks : BenchmarksBase
{
	private class Config : ManualConfig
	{
		public Config()
		{
			AddJob(Job.ShortRun);
		}
	}

	[ParamsAllValues]
	public bool OptEnabled;

	public override void Setup()
	{
		base.Setup();

		KeysetPaginationExtensions.EnableFirstColPredicateOpt = OptEnabled;
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
