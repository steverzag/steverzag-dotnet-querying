using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Pagination;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Aggregating
{
	/// <summary>The non-grouped <c>ApplySelect(query, selectors)</c>: one <see cref="SelectResult"/> per row.</summary>
	public class SelectTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 1 Hammer Qty 100 (2 reviews) | 2 Shovel Qty 0 (1) | 3 Wrench Qty 50 (0) | 4 Rake Qty 200 (3)

		private const string ReviewCount = "Reviews|" + SelectorDefaults.CountAggregateFunction;
		private const string MaxStars = "Reviews.Stars|" + SelectorDefaults.MaxAggregateFunction;

		private List<SelectResult> Select(List<string> selectors)
		{
			using var context = Database.CreateContext();
			return context.Products.OrderBy(p => p.Id).ApplySelect(selectors).ToList();
		}

		private static object? Value(SelectResult result, string label)
			=> Assert.Single(result.Values, v => v.Selector == label).Value;

		[Fact]
		public void OneResultPerRow_InQueryOrder()
		{
			var rows = Select([nameof(Product.Name), nameof(Product.Quantity)]);

			Assert.Equal(["Hammer", "Shovel", "Wrench", "Rake"], rows.Select(r => Value(r, nameof(Product.Name))));
			Assert.Equal([100, 0, 50, 200], rows.Select(r => Value(r, nameof(Product.Quantity))));
		}

		[Fact]
		public void NavigationProperty()
		{
			var rows = Select(["Category.Name"]);

			Assert.Equal(["Tools", "Garden", "Tools", "Garden"], rows.Select(r => Value(r, "Category.Name")));
		}

		[Fact]
		public void AggregateOverACollection()
		{
			var rows = Select([ReviewCount]);

			Assert.Equal([2, 1, 0, 3], rows.Select(r => Value(r, ReviewCount)));
		}

		[Fact]
		public void CountDistinctOverACollection()
		{
			const string distinctStars = "Reviews.Stars|" + SelectorDefaults.CountDistinctAggregateFunction;

			var rows = Select([distinctStars]);

			Assert.Equal([2, 1, 0, 2], rows.Select(r => Value(r, distinctStars)));
		}

		[Fact]
		public void CountDistinctOverACollection_SkipsNulls()
		{
			// Measurements: [1, null] | [2] | [] | [1, 2]
			const string distinctMeasurements = "Measurements|" + SelectorDefaults.CountDistinctAggregateFunction;

			var rows = Select([distinctMeasurements]);

			Assert.Equal([1, 1, 0, 2], rows.Select(r => Value(r, distinctMeasurements)));
		}

		[Fact]
		public void ValuesAreLabelledWithTheFullSelector()
		{
			var rows = Select([nameof(Product.Name), ReviewCount, MaxStars]);

			Assert.All(rows, r => Assert.Equal([nameof(Product.Name), ReviewCount, MaxStars], r.Values.Select(v => v.Selector)));
		}

		[Fact]
		public void NoSelectors_ReturnsOneEmptyResultPerRow()
		{
			var rows = Select([]);

			Assert.Equal(4, rows.Count);
			Assert.All(rows, r => Assert.Empty(r.Values));
		}

		[Fact]
		public void Paginate_AfterSelect()
		{
			using var context = Database.CreateContext();
			var page = context.Products.OrderBy(p => p.Id).ApplySelect([nameof(Product.Name)]).Paginate(2, 3);

			Assert.Equal(2, page.TotalPages);
			Assert.Equal(["Rake"], page.Select(r => Value(r, nameof(Product.Name))));
		}
	}
}
