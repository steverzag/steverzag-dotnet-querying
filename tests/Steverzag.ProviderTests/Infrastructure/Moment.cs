using Microsoft.EntityFrameworkCore;

namespace Steverzag.ProviderTests.Infrastructure
{
	/// <summary>One column per framework date/time type, plain and nullable. The probe reads every member of each.</summary>
	public class Moment
	{
		public int Id { get; set; }

		public DateTime At { get; set; }
		public DateTime? MaybeAt { get; set; }

		public DateTimeOffset AtOffset { get; set; }
		public DateTimeOffset? MaybeAtOffset { get; set; }

		public DateOnly Day { get; set; }
		public DateOnly? MaybeDay { get; set; }

		public TimeOnly Time { get; set; }
		public TimeOnly? MaybeTime { get; set; }

		public TimeSpan Duration { get; set; }
		public TimeSpan? MaybeDuration { get; set; }
	}

	public class MomentContext(DbContextOptions<MomentContext> options) : DbContext(options)
	{
		public DbSet<Moment> Moments => Set<Moment>();
		public DbSet<Scalar> Scalars => Set<Scalar>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Moment>().Property(m => m.Id).ValueGeneratedNever();
			modelBuilder.Entity<Scalar>().Property(s => s.Id).ValueGeneratedNever();
		}
	}

	public enum Level
	{
		Low = 1,
		High = 3,
		Mid = 2,
	}

	/// <summary>One column per common scalar type, for the ordering probe. Values are chosen so that text order differs from value order.</summary>
	public class Scalar
	{
		public int Id { get; set; }
		public bool Flag { get; set; }
		public byte Tiny { get; set; }
		public short Small { get; set; }
		public int Number { get; set; }
		public long Big { get; set; }
		public ulong Huge { get; set; }
		public float Single { get; set; }
		public double Real { get; set; }
		public decimal Amount { get; set; }
		public decimal? MaybeAmount { get; set; }
		public char Letter { get; set; }
		public string Text { get; set; } = "";
		public Guid Code { get; set; }
		public Level Level { get; set; }
		public byte[] Blob { get; set; } = [];

		public static List<Scalar> Rows() =>
		[
			new() { Id = 1, Flag = true, Tiny = 9, Small = -2, Number = 10, Big = 9_000_000_000, Huge = 18_446_744_073_709_551_615, Single = 2.5f, Real = 10.25, Amount = 10.5m, MaybeAmount = 10.5m, Letter = 'b', Text = "b", Code = Guid.Parse("00000000-0000-0000-0000-000000000002"), Level = Level.High, Blob = [2] },
			new() { Id = 2, Flag = false, Tiny = 10, Small = 3, Number = 9, Big = -1, Huge = 9_223_372_036_854_775_808, Single = -1f, Real = 9.5, Amount = 9.25m, MaybeAmount = 9.25m, Letter = 'a', Text = "ab", Code = Guid.Parse("01000000-0000-0000-0000-000000000000"), Level = Level.Low, Blob = [1, 5] },
			new() { Id = 3, Flag = true, Tiny = 1, Small = 100, Number = -5, Big = 42, Huge = 5, Single = 100f, Real = -3, Amount = -0.75m, MaybeAmount = null, Letter = 'c', Text = "a", Code = Guid.Parse("00000000-0000-0000-0000-000000000001"), Level = Level.Mid, Blob = [10] },
		];
	}

	public enum Provider
	{
		Sqlite,
		SqlServer,
		PostgreSql,
	}

	public static class MomentSeed
	{
		/// <summary>
		/// Values every provider can store. PostgreSQL's <c>timestamptz</c> only takes UTC, so its offsets are 0.
		/// SQL Server stores a <c>TimeSpan</c> as <c>time</c>, so its durations stay within one day.
		/// The probe compares against the rows as read back, so a provider's storage precision isn't counted as a failure.
		/// </summary>
		public static List<Moment> Rows(Provider provider)
		{
			var offsets = provider == Provider.PostgreSql
				? new[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero }
				: new[] { TimeSpan.FromHours(5), TimeSpan.Zero, TimeSpan.FromHours(-3.5), TimeSpan.FromHours(14) };

			var durations = provider == Provider.SqlServer
				? new[] { new TimeSpan(0, 23, 59, 59, 999), TimeSpan.Zero, new TimeSpan(0, 13, 45, 30, 123, 456), new TimeSpan(0, 6, 7, 8, 500) }
				: new[] { new TimeSpan(3, 23, 59, 59, 999), TimeSpan.Zero, new TimeSpan(0, 13, 45, 30, 123, 456), new TimeSpan(-2, -6, -7, -8, -500) };

			var instants = new[]
			{
				new DateTime(2024, 2, 29, 23, 59, 59, 999, DateTimeKind.Utc),
				new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				new DateTime(2026, 11, 27, 13, 45, 30, 123, 456, DateTimeKind.Utc),
				new DateTime(1999, 12, 31, 6, 7, 8, 500, DateTimeKind.Utc),
			};

			return [.. Enumerable.Range(0, 4).Select(i =>
			{
				var at = instants[i];
				var atOffset = new DateTimeOffset(DateTime.SpecifyKind(at, DateTimeKind.Unspecified), offsets[i]);
				var last = i == 3;
				return new Moment
				{
					Id = i + 1,
					At = at,
					MaybeAt = last ? null : at,
					AtOffset = atOffset,
					MaybeAtOffset = last ? null : atOffset,
					Day = DateOnly.FromDateTime(at),
					MaybeDay = last ? null : DateOnly.FromDateTime(at),
					Time = TimeOnly.FromDateTime(at),
					MaybeTime = last ? null : TimeOnly.FromDateTime(at),
					Duration = durations[i],
					MaybeDuration = last ? null : durations[i],
				};
			})];
		}
	}
}
