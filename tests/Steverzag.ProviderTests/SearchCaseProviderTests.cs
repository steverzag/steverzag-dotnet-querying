using Steverzag.ProviderTests.Infrastructure;
using Steverzag.QueryShaper.Filtering;

namespace Steverzag.ProviderTests
{
	/// <summary>
	/// The base search ignores case by default: a lookup matches the text as written, or its lowercase form matches the
	/// text's. Which letters fold, and whether <c>caseSensitive: true</c> can compare case at all, is the database's rule.
	/// </summary>
	public abstract class SearchCaseProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
		where TFixture : ProviderFixture
	{
		// Text: 1 "b", 2 "ab", 3 "a"
		private const string NonAsciiLookup = "$FN_STR_CONCAT(\"Ángel \", Text)";

		protected abstract bool FoldsNonAscii { get; }

		protected abstract bool CanCompareCase { get; }

		private int[] Ids(string lookup, string search, bool caseSensitive = false)
		{
			using var context = fixture.CreateContext();
			return context.Scalars.ApplyBaseFilter([lookup], search, caseSensitive).Select(s => s.Id).OrderBy(id => id).ToArray();
		}

		[Fact]
		public void AsciiCase_IsIgnored_ByDefault()
		{
			Assert.Equal([2], Ids(nameof(Scalar.Text), "AB"));
			Assert.Equal([1, 2], Ids(nameof(Scalar.Text), "B"));
		}

		[Fact]
		public void CaseSensitive_MatchesTheTextAsWritten_WhereTheDatabaseComparesCase()
		{
			Assert.Equal([2], Ids(nameof(Scalar.Text), "ab", caseSensitive: true));
			Assert.Equal(CanCompareCase ? [] : [2], Ids(nameof(Scalar.Text), "AB", caseSensitive: true));
		}

		[Fact]
		public void NonAscii_AsWritten_AlwaysMatches()
		{
			Assert.Equal([1, 2, 3], Ids(NonAsciiLookup, "Ángel"));
			Assert.Equal([2], Ids(NonAsciiLookup, "Ángel ab"));
		}

		[Fact]
		public void NonAsciiCase_IsIgnored_WhereTheDatabaseFoldsIt()
		{
			Assert.Equal(FoldsNonAscii ? [1, 2, 3] : [], Ids(NonAsciiLookup, "ÁNGEL"));
			Assert.Equal(FoldsNonAscii ? [2] : [], Ids(NonAsciiLookup, "ángel AB"));
		}
	}

	public sealed class SqliteSearchCaseTests(SqliteFixture fixture) : SearchCaseProviderTests<SqliteFixture>(fixture)
	{
		protected override bool FoldsNonAscii => false;

		protected override bool CanCompareCase => true;
	}

	// The default collation, SQL_Latin1_General_CP1_CI_AS, ignores case in every comparison.
	public sealed class SqlServerSearchCaseTests(SqlServerFixture fixture) : SearchCaseProviderTests<SqlServerFixture>(fixture)
	{
		protected override bool FoldsNonAscii => true;

		protected override bool CanCompareCase => false;
	}

	public sealed class PostgreSqlSearchCaseTests(PostgreSqlFixture fixture) : SearchCaseProviderTests<PostgreSqlFixture>(fixture)
	{
		protected override bool FoldsNonAscii => true;

		protected override bool CanCompareCase => true;
	}
}
