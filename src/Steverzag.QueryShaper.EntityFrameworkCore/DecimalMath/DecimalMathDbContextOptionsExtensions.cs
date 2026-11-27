using Microsoft.EntityFrameworkCore;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	/// <summary>Registers the decimal math fallback with a context's options.</summary>
	public static class DecimalMathDbContextOptionsExtensions
	{
		/// <summary>
		/// Lets SQLite run <c>ROUND</c>, <c>CEILING</c>, <c>FLOOR</c> and <c>TRUNCATE</c> on a <see cref="decimal"/>, which EF's
		/// SQLite provider can't translate, by computing them in <c>REAL</c>.
		/// </summary>
		/// <remarks>
		/// The rounding then has <see cref="double"/> precision on SQLite. Register it on SQLite only: on SQL Server and
		/// PostgreSQL, which translate these functions exactly, it would replace exact SQL with floating-point SQL.
		/// </remarks>
		/// <param name="optionsBuilder">The context's options.</param>
		/// <returns>The options, to chain more calls.</returns>
		/// <example><code>options.UseSqlite(connection).UseDecimalMathAsDouble();</code></example>
		public static DbContextOptionsBuilder UseDecimalMathAsDouble(this DbContextOptionsBuilder optionsBuilder)
		{
			ArgumentNullException.ThrowIfNull(optionsBuilder);
			return optionsBuilder.AddInterceptors(DecimalMathAsDoubleInterceptor.Instance);
		}

		/// <inheritdoc cref="UseDecimalMathAsDouble(DbContextOptionsBuilder)"/>
		/// <typeparam name="TContext">The context type.</typeparam>
		public static DbContextOptionsBuilder<TContext> UseDecimalMathAsDouble<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
			where TContext : DbContext
		{
			ArgumentNullException.ThrowIfNull(optionsBuilder);
			return optionsBuilder.AddInterceptors(DecimalMathAsDoubleInterceptor.Instance);
		}
	}
}
