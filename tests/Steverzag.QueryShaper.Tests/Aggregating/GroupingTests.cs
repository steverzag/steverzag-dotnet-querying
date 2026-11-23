using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Aggregating
{
	public class GroupingTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 1 Hammer | Tools  | Qty 100 | Active       | active   | tags 2 | review stars 5, 4
		// 2 Shovel | Garden | Qty 0   | Draft        | active   | tags 1 | review stars 3
		// 3 Wrench | Tools  | Qty 50  | Discontinued | inactive | tags 0 | no reviews
		// 4 Rake   | Garden | Qty 200 | Active       | active   | tags 2 | review stars 4, 4, 2

		private const string Key0 = "Key.GroupKey0";
		private const string Key1 = "Key.GroupKey1";
		private const string Count = SelectorDefaults.CountAggregateFunction;
		private const string QuantitySum = "Quantity|" + SelectorDefaults.SumAggregateFunction;
		private const string QuantityMin = "Quantity|" + SelectorDefaults.MinAggregateFunction;
		private const string QuantityMax = "Quantity|" + SelectorDefaults.MaxAggregateFunction;
		private const string QuantityAvg = "Quantity|" + SelectorDefaults.AvgAggregateFunction;

		private List<SelectResult> Grouped(List<PropertySelector<Product>> groupBy, List<string> select, bool onlyActive = false)
		{
			using var context = Database.CreateContext();
			IQueryable<Product> query = context.Products;
			if (onlyActive)
			{
				query = query.ApplyLogicsFilter([new FilterNode { FieldSelector = nameof(Product.IsActive), FilterCondition = FilterCondition.Equals, SearchValue = true }]);
			}

			return query.ApplyGrouping(groupBy).ApplySelect(select).ToList();
		}

		/// <summary>Finds the one result whose value under <paramref name="label"/> equals <paramref name="key"/>.</summary>
		private static SelectResult GroupWhere(List<SelectResult> results, string label, object key)
			=> Assert.Single(results, r => Equals(Value(r, label), key));

		private static object? Value(SelectResult result, string label)
			=> Assert.Single(result.Values, v => v.Selector == label).Value;

		// ---- one key

		[Fact]
		public void CountPerGroup()
		{
			var results = Grouped([nameof(Product.Status)], [Key0, Count]);

			Assert.Equal(3, results.Count);
			Assert.Equal(2, Value(GroupWhere(results, Key0, ProductStatus.Active), Count));
			Assert.Equal(1, Value(GroupWhere(results, Key0, ProductStatus.Draft), Count));
			Assert.Equal(1, Value(GroupWhere(results, Key0, ProductStatus.Discontinued), Count));
		}

		[Fact]
		public void SumMinMaxAvgPerGroup()
		{
			var results = Grouped(["Category.Name"], [Key0, QuantitySum, QuantityMin, QuantityMax, QuantityAvg]);

			var tools = GroupWhere(results, Key0, "Tools");
			Assert.Equal(150, Value(tools, QuantitySum));
			Assert.Equal(50, Value(tools, QuantityMin));
			Assert.Equal(100, Value(tools, QuantityMax));
			Assert.Equal(75.0, Value(tools, QuantityAvg));

			var garden = GroupWhere(results, Key0, "Garden");
			Assert.Equal(200, Value(garden, QuantitySum));
			Assert.Equal(0, Value(garden, QuantityMin));
			Assert.Equal(200, Value(garden, QuantityMax));
			Assert.Equal(100.0, Value(garden, QuantityAvg));
		}

		[Fact]
		public void ValuesAreLabelledWithTheFullSelector()
		{
			var results = Grouped(["Category.Name"], [Key0, QuantitySum, QuantityAvg]);

			Assert.All(results, r => Assert.Equal([Key0, QuantitySum, QuantityAvg], r.Values.Select(v => v.Selector)));
		}

		[Fact]
		public void GroupKeysKeepTheirType()
		{
			var byBool = Grouped([nameof(Product.IsActive)], [Key0]);
			var byEnum = Grouped([nameof(Product.Status)], [Key0]);
			var byInt = Grouped([nameof(Product.Quantity)], [Key0]);

			Assert.All(byBool, r => Assert.IsType<bool>(Value(r, Key0)));
			Assert.All(byEnum, r => Assert.IsType<ProductStatus>(Value(r, Key0)));
			Assert.All(byInt, r => Assert.IsType<int>(Value(r, Key0)));
		}

		[Fact]
		public void OnlyTheSelectedValuesAreReturned()
		{
			// Keys are not added automatically: without "Key.GroupKey0" the groups can't be told apart.
			var results = Grouped(["Category.Name"], [Count]);

			Assert.Equal(2, results.Count);
			Assert.All(results, r => Assert.Equal([Count], r.Values.Select(v => v.Selector)));
		}

		[Fact]
		public void NoSelectors_ReturnsOneEmptyResultPerGroup()
		{
			var results = Grouped(["Category.Name"], []);

			Assert.Equal(2, results.Count);
			Assert.All(results, r => Assert.Empty(r.Values));
		}

		// ---- several keys

		[Fact]
		public void TwoKeys()
		{
			var results = Grouped(["Category.Name", nameof(Product.IsActive)], [Key0, Key1, Count]);

			Assert.Equal(3, results.Count);
			Assert.Equal(2, Value(Assert.Single(results, r => Equals(Value(r, Key0), "Garden") && Equals(Value(r, Key1), true)), Count));
			Assert.Equal(1, Value(Assert.Single(results, r => Equals(Value(r, Key0), "Tools") && Equals(Value(r, Key1), true)), Count));
			Assert.Equal(1, Value(Assert.Single(results, r => Equals(Value(r, Key0), "Tools") && Equals(Value(r, Key1), false)), Count));
		}

		[Fact]
		public void TwentyOneKeys_IsTheMaximum()
		{
			List<PropertySelector<Product>> keys = [.. Enumerable.Repeat<PropertySelector<Product>>(nameof(Product.IsActive), 21)];

			Assert.Equal(2, Grouped(keys, [Count]).Count);
		}

		[Fact]
		public void MoreThanTwentyOneKeys_Throws()
		{
			// GroupByKeys has 21 slots. A 22nd key has no property to bind to; today that surfaces as
			// an ArgumentNullException from Expression.Bind rather than a descriptive error.
			List<PropertySelector<Product>> keys = [.. Enumerable.Repeat<PropertySelector<Product>>(nameof(Product.IsActive), 22)];

			Assert.Throws<ArgumentNullException>(() => Grouped(keys, [Count]));
		}

		[Fact]
		public void NoKeys_FailsInsideEfCore()
		{
			// ApplyGrouping([]) builds GroupBy(x => new GroupByKeys()) with no key columns, and EF Core's
			// translator crashes on it ("Sequence contains no elements"). See ROADMAP Phase 2: either reject an
			// empty key list up front or treat it as "aggregate the whole table".
			var exception = Assert.Throws<InvalidOperationException>(() => Grouped([], [Count, QuantitySum]));
			Assert.Contains("Sequence contains no elements", exception.Message);
		}

		// ---- computed keys and nested collections

		[Fact]
		public void ComputedKey_TagCount()
		{
			const string tagCount = "Tags|" + SelectorDefaults.CountAggregateFunction;
			var results = Grouped([tagCount], [Key0, Count]);

			Assert.Equal(2, Value(GroupWhere(results, Key0, 2), Count));
			Assert.Equal(1, Value(GroupWhere(results, Key0, 1), Count));
			Assert.Equal(1, Value(GroupWhere(results, Key0, 0), Count));
		}

		[Fact]
		public void AggregateOverANestedCollection()
		{
			// Reviews of every product in the group are flattened: Tools has 2 reviews (5, 4), Garden 4 (3, 4, 4, 2).
			const string reviewCount = "Reviews|" + SelectorDefaults.CountAggregateFunction;
			const string averageStars = "Reviews.Stars|" + SelectorDefaults.AvgAggregateFunction;
			var results = Grouped(["Category.Name"], [Key0, reviewCount, averageStars]);

			var tools = GroupWhere(results, Key0, "Tools");
			Assert.Equal(2, Value(tools, reviewCount));
			Assert.Equal(4.5, Value(tools, averageStars));

			var garden = GroupWhere(results, Key0, "Garden");
			Assert.Equal(4, Value(garden, reviewCount));
			Assert.Equal(3.25, Value(garden, averageStars));
		}

		// ---- composition

		[Fact]
		public void FilterThenGroup()
		{
			var results = Grouped(["Category.Name"], [Key0, Count], onlyActive: true);

			Assert.Equal(1, Value(GroupWhere(results, Key0, "Tools"), Count));
			Assert.Equal(2, Value(GroupWhere(results, Key0, "Garden"), Count));
		}
	}
}
