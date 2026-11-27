using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.MetaData;
using Steverzag.ProviderTests.Infrastructure;
using Steverzag.QueryShaper.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Steverzag.ProviderTests
{
	/// <summary>
	/// The member filters against a real database. Every path a filter leaves in the tree must filter correctly, and each
	/// provider preset must allow exactly the members the probe finds correct on that provider, so a provider update that
	/// changes what translates shows up here. The report test prints the whole matrix.
	/// </summary>
	public abstract class MemberFilterProviderTests<TFixture>(TFixture fixture, ITestOutputHelper output) : IClassFixture<TFixture>
		where TFixture : ProviderFixture
	{
		private MemberFilter Preset()
		{
			using var context = fixture.CreateContext();
			return ProviderMemberFilters.For(context);
		}

		private void AssertEveryPathFiltersCorrectly(MemberFilter filter)
		{
			var paths = Nodes(new PropertiesExplorer<Moment>().ExplorePropertiesTree(3, filter))
				.Select(n => n.Path)
				.Where(p => p != nameof(Moment.Id))
				.ToList();

			var failures = paths
				.Select(p => MemberProbe.Run(fixture, p))
				.Where(r => r.Outcome != ProbeOutcome.Ok)
				.ToList();

			Assert.True(paths.Count > 10, "The tree should list the columns and their members.");
			Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Select(f => $"{f.Selector}: {f.Outcome} {f.Detail}")));
		}

		private void AssertEveryAcceptedChainFiltersCorrectly(MemberFilter filter)
		{
			var chains = from type in MemberProbe.FrameworkTypes
						 from column in MemberProbe.Columns(type)
						 from first in MemberProbe.Members(type)
						 from second in MemberProbe.Members(first.PropertyType)
						 from selector in new[] { $"{column}.{first.Name}.{second.Name}", $"{column}.{first.Name}|{second.Name}" }
						 select selector;

			var failures = chains
				.Where(selector => Accepts(filter, selector))
				.Select(selector => MemberProbe.Run(fixture, selector))
				.Where(r => r.Outcome != ProbeOutcome.Ok)
				.ToList();

			Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Select(f => $"{f.Selector}: {f.Outcome} {f.Detail}")));
		}

		private static bool Accepts(MemberFilter filter, string selector)
		{
			try
			{
				filter.Parse<Moment>(selector);
				return true;
			}
			catch (SelectorParseException)
			{
				return false;
			}
		}

		[Fact]
		public void Portable_EveryChainItAccepts_FiltersCorrectly() => AssertEveryAcceptedChainFiltersCorrectly(MemberFilter.Portable);

		[Fact]
		public void TheProviderPreset_EveryChainItAccepts_FiltersCorrectly() => AssertEveryAcceptedChainFiltersCorrectly(Preset());

		[Fact]
		public void Portable_EveryPathInItsTree_FiltersCorrectly() => AssertEveryPathFiltersCorrectly(MemberFilter.Portable);

		[Fact]
		public void TheProviderPreset_EveryPathInItsTree_FiltersCorrectly() => AssertEveryPathFiltersCorrectly(Preset());

		[Fact]
		public void TheProviderPreset_AllowsExactlyTheMembersThatPass()
		{
			var preset = Preset();
			var mismatches = MemberProbe.Selectors()
				.GroupBy(s => (s.Type, s.Member))
				.Select(g => (g.Key.Type, g.Key.Member, Passes: g.All(s => MemberProbe.Run(fixture, s.Selector).Outcome == ProbeOutcome.Ok)))
				.Where(m => m.Passes != preset.Allows(m.Type, m.Member))
				.Select(m => $"{m.Type.Name}.{m.Member.Name}: {(m.Passes ? "passes but is hidden" : "is allowed but fails")}")
				.ToList();

			Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
		}

		[Fact]
		public void For_PicksThePresetFromTheContext()
		{
			var expected = fixture.Provider switch
			{
				Provider.Sqlite => ProviderMemberFilters.Sqlite,
				Provider.SqlServer => ProviderMemberFilters.SqlServer,
				_ => ProviderMemberFilters.PostgreSql,
			};

			using var context = fixture.CreateContext();
			Assert.Same(expected, ProviderMemberFilters.For(context));
			Assert.Same(expected, ProviderMemberFilters.For(context.Database.ProviderName!));
		}

		[Fact]
		public void Report()
		{
			var preset = Preset();
			foreach (var (type, member, selector) in MemberProbe.Selectors())
			{
				var result = MemberProbe.Run(fixture, selector);
				var portable = MemberFilter.Portable.Allows(type, member) ? "portable" : "-";
				var provider = preset.Allows(type, member) ? "preset" : "-";
				output.WriteLine($"PROBE|{fixture.Provider}|{selector}|{result.Outcome}|{portable}|{provider}|{result.Detail}");
			}

			foreach (var column in MemberProbe.AllColumns())
			{
				foreach (var comparison in MemberProbe.Comparisons.Cast<ExpressionType?>().Append(null))
				{
					if (comparison is null && column.StartsWith("Maybe"))
						continue;

					var result = MemberProbe.RunValue(fixture, column, comparison);
					output.WriteLine($"VALUE|{fixture.Provider}|{result.Selector}|{result.Outcome}|{result.Detail}");
				}
			}
		}

		private static IEnumerable<SelectorsMetaDataTree> Nodes(SelectorsMetaDataTree node) =>
			node.Selectors.SelectMany(child => Nodes(child).Prepend(child));
	}

	/// <summary>
	/// Whole values of the five types: <c>==</c>, <c>&gt;</c>, <c>&lt;</c> and ordering. The filters can't express these
	/// (they're about members), so this pins what the docs say about them.
	/// </summary>
	public abstract class WholeValueTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
		where TFixture : ProviderFixture
	{
		[Fact]
		public void WholeValues_CompareAndOrder_AsDocumented()
		{
			var unsupported = fixture.Provider == Provider.Sqlite ? new[] { nameof(Moment.AtOffset), nameof(Moment.Duration) } : [];
			var failures = new List<string>();

			foreach (var column in MemberProbe.AllColumns())
			{
				var limited = unsupported.Contains(column.Replace("Maybe", ""));
				foreach (var comparison in MemberProbe.Comparisons.Cast<ExpressionType?>().Append(null))
				{
					if (comparison is null && column.StartsWith("Maybe"))
						continue;

					var result = MemberProbe.RunValue(fixture, column, comparison);
					var expectOk = !limited || comparison == ExpressionType.Equal;
					if ((result.Outcome == ProbeOutcome.Ok) != expectOk)
						failures.Add($"{result.Selector}: {result.Outcome} {result.Detail}");
				}
			}

			Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
		}
	}

	public sealed class SqliteMemberFilterTests(SqliteFixture fixture, ITestOutputHelper output) : MemberFilterProviderTests<SqliteFixture>(fixture, output);

	public sealed class SqlServerMemberFilterTests(SqlServerFixture fixture, ITestOutputHelper output) : MemberFilterProviderTests<SqlServerFixture>(fixture, output);

	public sealed class PostgreSqlMemberFilterTests(PostgreSqlFixture fixture, ITestOutputHelper output) : MemberFilterProviderTests<PostgreSqlFixture>(fixture, output);

	public sealed class SqliteWholeValueTests(SqliteFixture fixture) : WholeValueTests<SqliteFixture>(fixture);

	public sealed class SqlServerWholeValueTests(SqlServerFixture fixture) : WholeValueTests<SqlServerFixture>(fixture);

	public sealed class PostgreSqlWholeValueTests(PostgreSqlFixture fixture) : WholeValueTests<PostgreSqlFixture>(fixture);

	public class PresetConsistencyTests
	{
		[Fact]
		public void Portable_IsExactlyWhatEveryProviderPresetAllows()
		{
			var presets = new[] { ProviderMemberFilters.Sqlite, ProviderMemberFilters.SqlServer, ProviderMemberFilters.PostgreSql };
			var mismatches = MemberProbe.FrameworkTypes
				.SelectMany(type => MemberProbe.Members(type).Select(member => (type, member)))
				.Where(m => MemberFilter.Portable.Allows(m.type, m.member) != presets.All(p => p.Allows(m.type, m.member)))
				.Select(m => $"{m.type.Name}.{m.member.Name}")
				.ToList();

			Assert.Empty(mismatches);
		}

		[Fact]
		public void ThePresets_LeaveOtherTypesAlone()
		{
			foreach (var preset in new[] { ProviderMemberFilters.Sqlite, ProviderMemberFilters.SqlServer, ProviderMemberFilters.PostgreSql })
			{
				Assert.True(preset.Allows(typeof(Moment), typeof(Moment).GetProperty(nameof(Moment.At))!));
				Assert.True(preset.Allows(typeof(string), typeof(string).GetProperty(nameof(string.Length))!));
			}
		}

		[Fact]
		public void For_RejectsAnUnknownProvider()
		{
			var ex = Assert.Throws<NotSupportedException>(() => ProviderMemberFilters.For("Pomelo.EntityFrameworkCore.MySql"));

			Assert.Contains("Pomelo.EntityFrameworkCore.MySql", ex.Message);
			Assert.Contains(nameof(MemberFilter.Portable), ex.Message);
			Assert.Throws<ArgumentNullException>(() => ProviderMemberFilters.For((string)null!));
			Assert.Throws<ArgumentNullException>(() => ProviderMemberFilters.For((DbContext)null!));
		}
	}
}
