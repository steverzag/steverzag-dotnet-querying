namespace Steverzag.QueryShaper.Tests.Infrastructure
{
	/// <summary>Base for tests that query the shared, seeded SQLite database.</summary>
	[Collection(SqliteDatabaseCollection.Name)]
	public abstract class DatabaseTestBase(SqliteDatabaseFixture database)
	{
		protected SqliteDatabaseFixture Database { get; } = database;
	}
}
