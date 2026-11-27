using System.Collections.Frozen;
using Microsoft.EntityFrameworkCore;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	public static class ProviderUnsortableTypes
	{
		public static IReadOnlySet<Type> Sqlite { get; } =
			new[] { typeof(decimal), typeof(ulong), typeof(DateTimeOffset), typeof(TimeSpan) }.ToFrozenSet();

		public static IReadOnlySet<Type> SqlServer { get; } = FrozenSet<Type>.Empty;

		public static IReadOnlySet<Type> PostgreSql { get; } = FrozenSet<Type>.Empty;

		public static IReadOnlySet<Type> For(string providerName)
		{
			ArgumentNullException.ThrowIfNull(providerName);
			return providerName switch
			{
				ProviderMemberFilters.SqliteProviderName => Sqlite,
				ProviderMemberFilters.SqlServerProviderName => SqlServer,
				ProviderMemberFilters.PostgreSqlProviderName => PostgreSql,
				_ => throw new NotSupportedException(
					$"There is no list of unsortable types for the provider '{providerName}'. Known providers: " +
					$"{ProviderMemberFilters.SqliteProviderName}, {ProviderMemberFilters.SqlServerProviderName}, {ProviderMemberFilters.PostgreSqlProviderName}."),
			};
		}

		public static IReadOnlySet<Type> For(DbContext context)
		{
			ArgumentNullException.ThrowIfNull(context);
			return For(context.Database.ProviderName ?? throw new InvalidOperationException("The context has no database provider configured."));
		}
	}
}
