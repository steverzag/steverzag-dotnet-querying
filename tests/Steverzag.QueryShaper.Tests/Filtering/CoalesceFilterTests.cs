using Microsoft.EntityFrameworkCore;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class CoalesceFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// 1 Hammer | NullableQty 100  | DeletedAt null       | CreatedAt 2024-01-10 | max stars 5
		// 2 Shovel | NullableQty null | DeletedAt null       | CreatedAt 2024-03-15 | max stars 3
		// 3 Wrench | NullableQty 0    | DeletedAt 2024-07-01 | CreatedAt 2024-06-20 | no reviews
		// 4 Rake   | NullableQty 200  | DeletedAt null       | CreatedAt 2024-09-05 | max stars 4

		private const string MaxStarsOrZero = "Reviews.Stars|" + SelectorDefaults.MaxAggregateFunction + "|" + SelectorDefaults.Coalesce + "(0)";

		[Fact]
		public void AggregateOverAnEmptyCollection_UsesTheDefault()
		{
			Assert.Equal([3], FilterIds(MaxStarsOrZero, FilterCondition.Equals, ToValue(0)));
			Assert.Equal([2, 3], FilterIds(MaxStarsOrZero, FilterCondition.LessThan, ToValue(4)));
		}

		[Fact]
		public void NullableProperty_WithALiteralDefault()
		{
			Assert.Equal([2, 3], FilterIds("NullableQuantity|" + SelectorDefaults.Coalesce + "(0)", FilterCondition.Equals, ToValue(0)));
		}

		[Fact]
		public void NullableDate_FallsBackToAnotherProperty()
		{
			var field = "DeletedAt|" + SelectorDefaults.Coalesce + "(CreatedAt)";

			Assert.Equal([3, 4], FilterIds(field, FilterCondition.GreaterThan, ToValue(new DateTime(2024, 6, 30))));
		}

		[Fact]
		public void TranslatesToSqlCoalesce()
		{
			using var context = Database.CreateContext();
			var sql = context.Products.ApplyLogicsFilter([Node(MaxStarsOrZero, FilterCondition.Equals, ToValue(0))]).ToQueryString();

			Assert.Contains("COALESCE", sql);
		}

		[Fact]
		public void OrderingByACoalescedAggregate()
		{
			using var context = Database.CreateContext();
			var ids = context.Products
				.ApplyOrder((PropertySelector)MaxStarsOrZero, OrderDirection.Ascending)
				.Select(p => p.Id)
				.ToArray();

			Assert.Equal([3, 2, 4, 1], ids);
		}
	}
}
