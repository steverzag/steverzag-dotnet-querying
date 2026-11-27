using Microsoft.EntityFrameworkCore;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	/// <summary>
	/// Member filters that allow the date and time members that filter correctly on one provider, for
	/// <see cref="QueryShaperOptions.MemberFilter"/>. Each is one date or time member deep, like
	/// <see cref="MemberFilter.Portable"/>, and leaves your own types alone.
	/// </summary>
	/// <remarks>
	/// The lists were measured on EF Core 9 with SQLite, SQL Server 2022 and PostgreSQL 17; the table is in
	/// <c>docs/query-shaper.md</c>. Add your own rules as usual, such as <c>ProviderMemberFilters.SqlServer.Hide&lt;User&gt;(u => u.PasswordHash)</c>.
	/// </remarks>
	public static class ProviderMemberFilters
	{
		private static readonly HashSet<Type> DateTimeTypes = [typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan)];

		private static MemberFilter OneDateTimeMemberDeep { get; } = MemberFilter.All.Except(context =>
			context.SourceType is { } source && DateTimeTypes.Contains(source) && DateTimeTypes.Contains(context.Type));

		/// <summary>
		/// The preset for SQLite. It allows more <see cref="DateTime"/> and <see cref="DateOnly"/> members than
		/// <see cref="MemberFilter.Portable"/>, and none of <see cref="DateTimeOffset"/>, <see cref="TimeOnly"/> or
		/// <see cref="TimeSpan"/>, which SQLite doesn't translate.
		/// </summary>
		public static MemberFilter Sqlite { get; } = OneDateTimeMemberDeep
			.Only<DateTime>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute, d => d.Second, d => d.Millisecond,
				d => d.Date, d => d.DayOfWeek, d => d.DayOfYear)
			.Only<DateOnly>(d => d.Year, d => d.Month, d => d.Day, d => d.DayOfWeek, d => d.DayOfYear)
			.Except((type, _) => type == typeof(DateTimeOffset) || type == typeof(TimeOnly) || type == typeof(TimeSpan));

		/// <summary>The preset for SQL Server, which translates members of all five date and time types.</summary>
		public static MemberFilter SqlServer { get; } = OneDateTimeMemberDeep
			.Only<DateTime>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute, d => d.Second, d => d.Millisecond,
				d => d.Date, d => d.DayOfYear, d => d.TimeOfDay)
			.Only<DateTimeOffset>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute, d => d.Second, d => d.Millisecond,
				d => d.Date, d => d.DayOfYear, d => d.TimeOfDay)
			.Only<DateOnly>(d => d.Year, d => d.Month, d => d.Day, d => d.DayOfYear)
			.Only<TimeOnly>(t => t.Hour, t => t.Minute, t => t.Second, t => t.Millisecond)
			.Only<TimeSpan>(t => t.Hours, t => t.Minutes, t => t.Seconds, t => t.Milliseconds);

		/// <summary>The preset for PostgreSQL, which translates members of all five date and time types.</summary>
		public static MemberFilter PostgreSql { get; } = OneDateTimeMemberDeep
			.Only<DateTime>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute,
				d => d.Date, d => d.DayOfWeek, d => d.DayOfYear, d => d.TimeOfDay)
			.Only<DateTimeOffset>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute,
				d => d.Date, d => d.DayOfWeek, d => d.DayOfYear, d => d.TimeOfDay, d => d.DateTime, d => d.UtcDateTime)
			.Only<DateOnly>(d => d.Year, d => d.Month, d => d.Day, d => d.DayOfWeek, d => d.DayOfYear, d => d.DayNumber)
			.Only<TimeOnly>(t => t.Hour, t => t.Minute)
			.Only<TimeSpan>(t => t.Days, t => t.Hours, t => t.Minutes, t => t.Milliseconds,
				t => t.TotalDays, t => t.TotalHours, t => t.TotalSeconds, t => t.TotalMilliseconds);

		/// <summary>Returns the preset for a provider.</summary>
		/// <param name="providerName">The provider's name, one of the <see cref="ProviderNames"/>.</param>
		/// <returns>The preset.</returns>
		/// <exception cref="NotSupportedException">There is no preset for the provider; use <see cref="MemberFilter.Portable"/> instead.</exception>
		public static MemberFilter For(string providerName)
		{
			ArgumentNullException.ThrowIfNull(providerName);
			return providerName switch
			{
				ProviderNames.Sqlite => Sqlite,
				ProviderNames.SqlServer => SqlServer,
				ProviderNames.PostgreSql => PostgreSql,
				_ => throw new NotSupportedException(
					$"There is no member filter for the provider '{providerName}'. Known providers: {ProviderNames.Sqlite}, {ProviderNames.SqlServer}, {ProviderNames.PostgreSql}. " +
					$"Use {nameof(MemberFilter)}.{nameof(MemberFilter.Portable)} for members that work on all three."),
			};
		}

		/// <summary>Returns the preset for the provider a context uses.</summary>
		/// <param name="context">The context.</param>
		/// <returns>The preset.</returns>
		/// <exception cref="InvalidOperationException">The context has no provider configured.</exception>
		/// <exception cref="NotSupportedException">There is no preset for the provider; use <see cref="MemberFilter.Portable"/> instead.</exception>
		public static MemberFilter For(DbContext context)
		{
			ArgumentNullException.ThrowIfNull(context);
			return For(context.Database.ProviderName ?? throw new InvalidOperationException("The context has no database provider configured."));
		}
	}
}
