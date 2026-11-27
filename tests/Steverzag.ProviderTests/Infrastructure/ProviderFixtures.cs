using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace Steverzag.ProviderTests.Infrastructure
{
	/// <summary>A seeded database for one provider. SQL Server and PostgreSQL run in containers, so Docker must be running.</summary>
	public abstract class ProviderFixture : IAsyncLifetime
	{
		private DbContextOptions<MomentContext>? _options;

		public abstract Provider Provider { get; }

		public List<Moment> Rows { get; private set; } = [];

		public MomentContext CreateContext()
		{
			var context = new MomentContext(_options ?? throw new InvalidOperationException("The database isn't started."));
			context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			return context;
		}

		public async Task InitializeAsync()
		{
			_options = await StartAsync();

			await using (var context = CreateContext())
			{
				await context.Database.EnsureCreatedAsync();
				context.Moments.AddRange(MomentSeed.Rows(Provider));
				context.Scalars.AddRange(Scalar.Rows());
				await context.SaveChangesAsync();
			}

			await using (var context = CreateContext())
			{
				Rows = await context.Moments.OrderBy(m => m.Id).ToListAsync();
				Scalars = await context.Scalars.OrderBy(s => s.Id).ToListAsync();
			}
		}

		public List<Scalar> Scalars { get; private set; } = [];

		protected abstract Task<DbContextOptions<MomentContext>> StartAsync();

		public abstract Task DisposeAsync();
	}

	public sealed class SqliteFixture : ProviderFixture
	{
		private readonly SqliteConnection _connection = new("DataSource=:memory:");

		public override Provider Provider => Provider.Sqlite;

		protected override async Task<DbContextOptions<MomentContext>> StartAsync()
		{
			await _connection.OpenAsync();
			return new DbContextOptionsBuilder<MomentContext>().UseSqlite(_connection).Options;
		}

		public override Task DisposeAsync() => _connection.DisposeAsync().AsTask();
	}

	public sealed class SqlServerFixture : ProviderFixture
	{
		private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

		public override Provider Provider => Provider.SqlServer;

		protected override async Task<DbContextOptions<MomentContext>> StartAsync()
		{
			await _container.StartAsync();
			return new DbContextOptionsBuilder<MomentContext>().UseSqlServer(_container.GetConnectionString()).Options;
		}

		public override Task DisposeAsync() => _container.DisposeAsync().AsTask();
	}

	public sealed class PostgreSqlFixture : ProviderFixture
	{
		private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

		public override Provider Provider => Provider.PostgreSql;

		protected override async Task<DbContextOptions<MomentContext>> StartAsync()
		{
			await _container.StartAsync();
			return new DbContextOptionsBuilder<MomentContext>().UseNpgsql(_container.GetConnectionString()).Options;
		}

		public override Task DisposeAsync() => _container.DisposeAsync().AsTask();
	}
}
