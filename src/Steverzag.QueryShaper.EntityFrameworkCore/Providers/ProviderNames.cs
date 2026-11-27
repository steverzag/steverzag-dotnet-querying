namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	/// <summary>The EF Core provider names the presets know, as <c>Database.ProviderName</c> reports them.</summary>
	public static class ProviderNames
	{
		/// <summary>EF Core's SQLite provider.</summary>
		public const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";
		/// <summary>EF Core's SQL Server provider.</summary>
		public const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
		/// <summary>Npgsql's PostgreSQL provider.</summary>
		public const string PostgreSql = "Npgsql.EntityFrameworkCore.PostgreSQL";
	}
}
