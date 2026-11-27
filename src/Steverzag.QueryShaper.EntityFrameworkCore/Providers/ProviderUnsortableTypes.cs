using System.Collections.Frozen;
using Microsoft.EntityFrameworkCore;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	/// <summary>
	/// The types each provider can't sort, for <see cref="QueryShaperOptions.UnsortableTypes"/>, so an order on one is a
	/// request error instead of a failure when the query runs.
	/// </summary>
	/// <example><code>services.AddQueryShaper&lt;AppConfig&gt;(o => o.UnsortableTypes.UnionWith(ProviderUnsortableTypes.Sqlite));</code></example>
	public static class ProviderUnsortableTypes
	{
		/// <summary>The types SQLite can't sort: <see cref="decimal"/>, <see cref="ulong"/>, <see cref="DateTimeOffset"/> and <see cref="TimeSpan"/>.</summary>
		public static IReadOnlySet<Type> Sqlite { get; } =
			new[] { typeof(decimal), typeof(ulong), typeof(DateTimeOffset), typeof(TimeSpan) }.ToFrozenSet();

		/// <summary>The types SQL Server can't sort: none.</summary>
		public static IReadOnlySet<Type> SqlServer { get; } = FrozenSet<Type>.Empty;

		/// <summary>The types PostgreSQL can't sort: none.</summary>
		public static IReadOnlySet<Type> PostgreSql { get; } = FrozenSet<Type>.Empty;

		/// <summary>Returns the types a provider can't sort.</summary>
		/// <param name="providerName">The provider's name, one of the <see cref="ProviderNames"/>.</param>
		/// <returns>The types.</returns>
		/// <exception cref="NotSupportedException">The provider isn't one of the <see cref="ProviderNames"/>.</exception>
		public static IReadOnlySet<Type> For(string providerName)
		{
			ArgumentNullException.ThrowIfNull(providerName);
			return providerName switch
			{
				ProviderNames.Sqlite => Sqlite,
				ProviderNames.SqlServer => SqlServer,
				ProviderNames.PostgreSql => PostgreSql,
				_ => throw new NotSupportedException(
					$"There is no list of unsortable types for the provider '{providerName}'. Known providers: " +
					$"{ProviderNames.Sqlite}, {ProviderNames.SqlServer}, {ProviderNames.PostgreSql}."),
			};
		}

		/// <summary>Returns the types the provider a context uses can't sort.</summary>
		/// <param name="context">The context.</param>
		/// <returns>The types.</returns>
		/// <exception cref="InvalidOperationException">The context has no provider configured.</exception>
		/// <exception cref="NotSupportedException">The provider isn't one of the <see cref="ProviderNames"/>.</exception>
		public static IReadOnlySet<Type> For(DbContext context)
		{
			ArgumentNullException.ThrowIfNull(context);
			return For(context.Database.ProviderName ?? throw new InvalidOperationException("The context has no database provider configured."));
		}
	}
}
