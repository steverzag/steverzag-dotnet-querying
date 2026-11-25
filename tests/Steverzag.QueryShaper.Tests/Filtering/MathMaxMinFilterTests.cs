using Microsoft.EntityFrameworkCore;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class MathMaxMinFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// id | Quantity | NullableQuantity | Price
		//  1 | 100      | 100              | 19.99
		//  2 | 0        | null             | 0
		//  3 | 50       | 0                | 9.99
		//  4 | 200      | 200              | 29.99

		private List<TResult> Values<TResult>(string text)
		{
			PropertySelector<Product> selector = text;
			var parameter = Expression.Parameter(typeof(Product), "p");
			var lambda = Expression.Lambda<Func<Product, TResult>>(ExpressionUtils.CreateMemberAccess(parameter, selector), parameter);

			using var context = Database.CreateContext();
			return context.Products.OrderBy(p => p.Id).Select(lambda).ToList();
		}

		[Fact]
		public void DecimalMax_ComparesNumerically()
		{
			Assert.Equal([2, 3], FilterIds("Price|$FN_MATH_MAX(10)", FilterCondition.Equals, 10m));
			Assert.Equal([1, 4], FilterIds("Price|$FN_MATH_MAX(10)", FilterCondition.GreaterThan, 10m));
		}

		[Fact]
		public void DecimalMin_ComparesNumerically()
		{
			Assert.Equal([2, 3], FilterIds("$FN_MATH_MIN(Price, 10)", FilterCondition.LessThan, 10m));
		}

		[Fact]
		public void DecimalMax_IsAnExactComparison()
		{
			using var context = Database.CreateContext();
			var sql = context.Products.ApplyLogicsFilter([Node("Price|$FN_MATH_MAX(1)", FilterCondition.Equals, 20m)]).ToQueryString();

			Assert.Contains("WHEN ef_compare(\"p\".\"Price\", '1.0') >= 0 THEN \"p\".\"Price\"", sql);
			Assert.DoesNotContain("REAL", sql);
		}

		[Fact]
		public void ANullOperand_IsIgnored()
		{
			Assert.Equal([2, 3], FilterIds("NullableQuantity|$FN_MATH_MIN(Quantity)", FilterCondition.Equals, 0));
			Assert.Equal([100, 0, 50, 200], Values<int>("NullableQuantity|$FN_MATH_MAX(Quantity)"));
		}

		[Fact]
		public void TheResultIsNull_OnlyWhenBothOperandsAre()
		{
			Assert.Equal([100, null, 0, 200], Values<int?>("$FN_MATH_MAX(NullableQuantity, NullableQuantity)"));
		}
	}
}
