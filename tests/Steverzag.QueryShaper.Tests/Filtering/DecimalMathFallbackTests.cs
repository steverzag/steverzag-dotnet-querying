using Microsoft.EntityFrameworkCore;
using Steverzag.QueryShaper.EntityFrameworkCore;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class DecimalMathFallbackTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// Prices: 1 → 19.99, 2 → 0, 3 → 9.99, 4 → 29.99

		private string Sql(string field)
		{
			using var context = Database.CreateContext();
			return context.Products.ApplyLogicsFilter([Node(field, FilterCondition.Equals, 20)]).ToQueryString();
		}

		private static TestDbContext ContextWithoutTheFallback()
			=> new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite("DataSource=:memory:").Options);

		[Theory]
		[InlineData("Price|$FN_MATH_ROUND(1)", "round(CAST(\"p\".\"Price\" AS REAL), 1)")]
		[InlineData("Price|$FN_MATH_CEILING", "ceiling(CAST(\"p\".\"Price\" AS REAL))")]
		[InlineData("Price|$FN_MATH_FLOOR", "floor(CAST(\"p\".\"Price\" AS REAL))")]
		[InlineData("Price|$FN_MATH_TRUNCATE", "trunc(CAST(\"p\".\"Price\" AS REAL))")]
		public void WithTheFallback_DecimalRoundingRunsThroughReal(string field, string expected)
		{
			Assert.Contains(expected, Sql(field));
		}

		[Theory]
		[InlineData("Price|$FN_MATH_ROUND(1)")]
		[InlineData("Price|$FN_MATH_CEILING")]
		public void WithoutTheFallback_DecimalRoundingFailsLoudly(string field)
		{
			using var context = ContextWithoutTheFallback();

			var exception = Assert.Throws<InvalidOperationException>(() =>
				context.Products.ApplyLogicsFilter([Node(field, FilterCondition.Equals, 20)]).ToQueryString());

			Assert.Contains("could not be translated", exception.Message);
		}

		[Fact]
		public void WithTheFallback_ComparisonsAreNumeric()
		{
			Assert.Equal([1, 4], FilterIds("Price|$FN_MATH_ROUND", FilterCondition.GreaterThan, 10));
			Assert.Equal([2, 3], FilterIds("Price|$FN_MATH_ROUND", FilterCondition.LessThan, 11));
		}

		[Theory]
		[InlineData("Score|$FN_MATH_ROUND(1)", "round(\"p\".\"Score\", 1)")]
		[InlineData("Price|$FN_MATH_MAX(1)", "max(CAST(\"p\".\"Price\" AS REAL), 1.0)")]
		public void TheFallback_LeavesOtherMathAlone(string field, string expected)
		{
			var sql = Sql(field);

			Assert.Contains(expected, sql);
			Assert.DoesNotContain("AS TEXT", sql);
		}
	}
}
