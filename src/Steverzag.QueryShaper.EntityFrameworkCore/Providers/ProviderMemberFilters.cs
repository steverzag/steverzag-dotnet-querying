using Microsoft.EntityFrameworkCore;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	public static class ProviderMemberFilters
	{
		public const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";
		public const string SqlServerProviderName = "Microsoft.EntityFrameworkCore.SqlServer";
		public const string PostgreSqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

		private static readonly HashSet<Type> DateTimeTypes = [typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan)];

		private static MemberFilter OneDateTimeMemberDeep { get; } = MemberFilter.All.Except(context =>
			context.SourceType is { } source && DateTimeTypes.Contains(source) && DateTimeTypes.Contains(context.Type));

		public static MemberFilter Sqlite { get; } = OneDateTimeMemberDeep
			.Only<DateTime>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute, d => d.Second, d => d.Millisecond,
				d => d.Date, d => d.DayOfWeek, d => d.DayOfYear)
			.Only<DateOnly>(d => d.Year, d => d.Month, d => d.Day, d => d.DayOfWeek, d => d.DayOfYear)
			.Except((type, _) => type == typeof(DateTimeOffset) || type == typeof(TimeOnly) || type == typeof(TimeSpan));

		public static MemberFilter SqlServer { get; } = OneDateTimeMemberDeep
			.Only<DateTime>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute, d => d.Second, d => d.Millisecond,
				d => d.Date, d => d.DayOfYear, d => d.TimeOfDay)
			.Only<DateTimeOffset>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute, d => d.Second, d => d.Millisecond,
				d => d.Date, d => d.DayOfYear, d => d.TimeOfDay)
			.Only<DateOnly>(d => d.Year, d => d.Month, d => d.Day, d => d.DayOfYear)
			.Only<TimeOnly>(t => t.Hour, t => t.Minute, t => t.Second, t => t.Millisecond)
			.Only<TimeSpan>(t => t.Hours, t => t.Minutes, t => t.Seconds, t => t.Milliseconds);

		public static MemberFilter PostgreSql { get; } = OneDateTimeMemberDeep
			.Only<DateTime>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute,
				d => d.Date, d => d.DayOfWeek, d => d.DayOfYear, d => d.TimeOfDay)
			.Only<DateTimeOffset>(d => d.Year, d => d.Month, d => d.Day, d => d.Hour, d => d.Minute,
				d => d.Date, d => d.DayOfWeek, d => d.DayOfYear, d => d.TimeOfDay, d => d.DateTime, d => d.UtcDateTime)
			.Only<DateOnly>(d => d.Year, d => d.Month, d => d.Day, d => d.DayOfWeek, d => d.DayOfYear, d => d.DayNumber)
			.Only<TimeOnly>(t => t.Hour, t => t.Minute)
			.Only<TimeSpan>(t => t.Days, t => t.Hours, t => t.Minutes, t => t.Milliseconds,
				t => t.TotalDays, t => t.TotalHours, t => t.TotalSeconds, t => t.TotalMilliseconds);

		public static MemberFilter For(string providerName)
		{
			ArgumentNullException.ThrowIfNull(providerName);
			return providerName switch
			{
				SqliteProviderName => Sqlite,
				SqlServerProviderName => SqlServer,
				PostgreSqlProviderName => PostgreSql,
				_ => throw new NotSupportedException(
					$"There is no member filter for the provider '{providerName}'. Known providers: {SqliteProviderName}, {SqlServerProviderName}, {PostgreSqlProviderName}. " +
					$"Use {nameof(MemberFilter)}.{nameof(MemberFilter.Portable)} for members that work on all three."),
			};
		}

		public static MemberFilter For(DbContext context)
		{
			ArgumentNullException.ThrowIfNull(context);
			return For(context.Database.ProviderName ?? throw new InvalidOperationException("The context has no database provider configured."));
		}
	}
}
