using Microsoft.Extensions.DependencyInjection;
using Steverzag.ProviderTests.Infrastructure;
using Steverzag.QueryShaper;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.EntityFrameworkCore;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.ProviderTests
{
	public sealed class NoConfiguration : QueryShaperConfiguration
	{
		protected override void OnConfiguring(QueryShaperConfigurationBuilder builder)
		{
		}
	}

	/// <summary>
	/// <see cref="ProviderUnsortableTypes"/> against a real database: each provider's set is exactly the probed types it
	/// can't order, and with it every order through <see cref="IQueryShaper"/> is either rejected as a request error or runs.
	/// </summary>
	public abstract class OrderGuardProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
		where TFixture : ProviderFixture
	{
		private IReadOnlySet<Type> Unsortable()
		{
			using var context = fixture.CreateContext();
			return ProviderUnsortableTypes.For(context);
		}

		private static IEnumerable<string> MomentColumns()
			=> MemberProbe.AllColumns().Where(c => !c.StartsWith("Maybe"));

		private static Type MomentType(string column) => typeof(Moment).GetProperty(column)!.PropertyType;

		[Fact]
		public void TheSet_IsExactlyTheTypesTheDatabaseCantOrder()
		{
			static bool Fails(ProbeResult r) => r.Outcome is ProbeOutcome.Error or ProbeOutcome.NotTranslated;

			var failing = OrderProbe.Columns().Where(c => Fails(OrderProbe.Run(fixture, c))).Select(OrderProbe.ColumnType)
				.Concat(MomentColumns().Where(c => Fails(MemberProbe.RunValue(fixture, c, null))).Select(MomentType))
				.ToHashSet();

			Assert.True(failing.SetEquals(Unsortable()),
				$"Can't order: {string.Join(", ", failing.Select(t => t.Name).Order())}. The set: {string.Join(", ", Unsortable().Select(t => t.Name).Order())}.");
		}

		[Fact]
		public void WithTheSet_EveryOrderIsRejectedUpFront_OrRuns()
		{
			var shaper = new ServiceCollection()
				.AddQueryShaper<NoConfiguration>(o => o.UnsortableTypes.UnionWith(Unsortable()))
				.BuildServiceProvider()
				.GetRequiredService<IQueryShaper>();
			var failures = new List<string>();

			foreach (var column in OrderProbe.Columns())
			{
				using var context = fixture.CreateContext();
				Check(column, () => shaper.ApplyOrder(context.Scalars, [new OrderParameters(column)]).ToList(), OrderProbe.ColumnType(column));
			}
			foreach (var column in MomentColumns())
			{
				using var context = fixture.CreateContext();
				Check(column, () => shaper.ApplyOrder(context.Moments, [new OrderParameters(column, OrderDirection.Descending)]).ToList(), MomentType(column));
			}

			Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));

			void Check(string column, Action run, Type type)
			{
				try
				{
					run();
					if (Unsortable().Contains(type))
						failures.Add($"{column}: ran, but {type.Name} is in the set");
				}
				catch (QueryRequestException) when (Unsortable().Contains(type))
				{
				}
				catch (Exception ex)
				{
					failures.Add($"{column}: {ex.GetType().Name}: {ex.Message}");
				}
			}
		}
	}

	public sealed class SqliteOrderGuardTests(SqliteFixture fixture) : OrderGuardProviderTests<SqliteFixture>(fixture);

	public sealed class SqlServerOrderGuardTests(SqlServerFixture fixture) : OrderGuardProviderTests<SqlServerFixture>(fixture);

	public sealed class PostgreSqlOrderGuardTests(PostgreSqlFixture fixture) : OrderGuardProviderTests<PostgreSqlFixture>(fixture);
}
