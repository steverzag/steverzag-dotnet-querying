using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Steverzag.QueryShaper.EntityFrameworkCore;

namespace Steverzag.QueryShaper.Tests.Infrastructure
{
	/// <summary>
	/// One in-memory SQLite database, created and seeded once, shared by every test in the
	/// <see cref="SqliteDatabaseCollection"/>. SQLite is used rather than EF InMemory so that each
	/// filter is translated to SQL: a filter that only works in LINQ-to-objects fails here.
	/// The database lives as long as the connection stays open.
	/// Tests must treat it as read-only.
	/// </summary>
	public sealed class SqliteDatabaseFixture : IDisposable
	{
		private readonly SqliteConnection _connection;
		private readonly DbContextOptions<TestDbContext> _options;

		public SqliteDatabaseFixture()
		{
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();

			_options = new DbContextOptionsBuilder<TestDbContext>()
				.UseSqlite(_connection)
				.UseDecimalMathAsDouble()
				.Options;

			using var context = CreateContext();
			context.Database.EnsureCreated();
			context.Categories.AddRange(SeedData.Categories());
			context.Products.AddRange(SeedData.Products());
			context.SaveChanges();
		}

		/// <summary>A fresh, non-tracking context over the shared database. Dispose it after use.</summary>
		public TestDbContext CreateContext()
		{
			var context = new TestDbContext(_options);
			context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
			return context;
		}

		public void Dispose() => _connection.Dispose();
	}

	/// <summary>
	/// Tests in this collection share one connection, so xUnit runs them one at a time.
	/// </summary>
	[CollectionDefinition(Name)]
	public sealed class SqliteDatabaseCollection : ICollectionFixture<SqliteDatabaseFixture>
	{
		public const string Name = "SQLite database";
	}
}
