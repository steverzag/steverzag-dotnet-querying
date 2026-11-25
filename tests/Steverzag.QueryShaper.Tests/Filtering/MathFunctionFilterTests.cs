using Microsoft.EntityFrameworkCore;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class MathFunctionFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// 1 Hammer | Price 19.99 | Quantity 100 | Score 4.5
		// 2 Shovel | Price 0     | Quantity 0   | Score 0
		// 3 Wrench | Price 9.99  | Quantity 50  | Score 2.25
		// 4 Rake   | Price 29.99 | Quantity 200 | Score 5

		[Theory]
		[InlineData("Price|$FN_MATH_ROUND", 20)]
		[InlineData("$FN_MATH_ROUND(Price)", 20)]
		[InlineData("Price|$FN_MATH_ROUND(1)", 20)]
		[InlineData("Price|$FN_MATH_CEILING", 20)]
		[InlineData("Price|$FN_MATH_FLOOR", 19)]
		[InlineData("Price|$FN_MATH_TRUNCATE", 19)]
		public void DecimalMath_Translates(string field, int expected)
		{
			Assert.Equal([1], FilterIds(field, FilterCondition.Equals, ToValue(expected)));
		}

		[Theory]
		[InlineData("Quantity|$FN_MATH_MAX(60)", FilterCondition.Equals, 60, new[] { 2, 3 })]
		[InlineData("$FN_MATH_MAX(Quantity, 60)", FilterCondition.Equals, 60, new[] { 2, 3 })]
		[InlineData("Quantity|$FN_MATH_MIN(60)", FilterCondition.LessThan, 60, new[] { 2, 3 })]
		// SQLite rounds halves away from zero and .NET to even, so this avoids Score 4.5.
		[InlineData("Score|$FN_MATH_ROUND", FilterCondition.Equals, 2, new[] { 3 })]
		public void IntegerAndDoubleMath_Translates(string field, FilterCondition condition, int value, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, condition, ToValue(value)));
		}

		[Fact]
		public void IntegerMax_IsNotConvertedToDouble()
		{
			using var context = Database.CreateContext();
			var sql = context.Products.ApplyLogicsFilter([Node("Quantity|$FN_MATH_MAX(60)", FilterCondition.Equals, ToValue(60))]).ToQueryString();

			Assert.DoesNotContain("REAL", sql);
		}
	}
}
