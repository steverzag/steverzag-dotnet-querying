using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Ordering
{
	public class OrderingTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 1 Hammer | Tools  | Qty 100 | NullableQty 100  | Jan | Active       | active   | 2 reviews (avg 4.5)
		// 2 Shovel | Garden | Qty 0   | NullableQty null | Mar | Draft        | active   | 1 review  (avg 3)
		// 3 Wrench | Tools  | Qty 50  | NullableQty 0    | Jun | Discontinued | inactive | no reviews
		// 4 Rake   | Garden | Qty 200 | NullableQty 200  | Sep | Active       | active   | 3 reviews (avg 3.33)

		private const OrderDirection Asc = OrderDirection.Ascending;
		private const OrderDirection Desc = OrderDirection.Descending;

		private int[] OrderIds(params OrderParameters[] orderParameters)
		{
			using var context = Database.CreateContext();
			return context.Products
				.ApplyOrder(orderParameters)
				.Select(p => p.Id)
				.ToArray();
		}

		// ---- a single key

		[Theory]
		[InlineData(nameof(Product.Name), Asc, new[] { 1, 4, 2, 3 })]
		[InlineData(nameof(Product.Name), Desc, new[] { 3, 2, 4, 1 })]
		[InlineData(nameof(Product.Quantity), Asc, new[] { 2, 3, 1, 4 })]
		[InlineData(nameof(Product.Quantity), Desc, new[] { 4, 1, 3, 2 })]
		[InlineData(nameof(Product.CreatedAt), Asc, new[] { 1, 2, 3, 4 })]
		[InlineData(nameof(Product.CreatedAt), Desc, new[] { 4, 3, 2, 1 })]
		[InlineData(nameof(Product.ReleaseDate), Desc, new[] { 4, 3, 2, 1 })]
		[InlineData(nameof(Product.OpensAt), Asc, new[] { 1, 2, 3, 4 })]
		public void SingleKey(string field, OrderDirection direction, int[] expected)
		{
			Assert.Equal(expected, OrderIds(new OrderParameters(field, direction)));
		}

		[Fact]
		public void SingleKey_SelectorOverload()
		{
			using var context = Database.CreateContext();
			var ids = context.Products
				.ApplyOrder((PropertySelector)nameof(Product.Name), Desc)
				.Select(p => p.Id)
				.ToArray();

			Assert.Equal([3, 2, 4, 1], ids);
		}

		[Fact]
		public void NavigationProperty()
		{
			// Garden: Shovel, Rake. Tools: Hammer, Wrench. Ties broken by Id to keep the test deterministic.
			Assert.Equal([2, 4, 1, 3], OrderIds(new OrderParameters("Category.Name", Asc), new OrderParameters(nameof(Product.Id), Asc)));
		}

		// ---- several keys (ThenBy)

		[Fact]
		public void SecondKey_BreaksTies()
		{
			// Status: Draft (2), Active (1 Hammer, 4 Rake), Discontinued (3)
			Assert.Equal([2, 1, 4, 3], OrderIds(new OrderParameters(nameof(Product.Status), Asc), new OrderParameters(nameof(Product.Name), Asc)));
		}

		[Fact]
		public void SecondKey_HasItsOwnDirection()
		{
			Assert.Equal([2, 4, 1, 3], OrderIds(new OrderParameters(nameof(Product.Status), Asc), new OrderParameters(nameof(Product.Name), Desc)));
		}

		[Fact]
		public void NavigationThenScalar_MixedDirections()
		{
			// Tools desc first: Wrench (50), Hammer (100). Then Garden: Shovel (0), Rake (200).
			Assert.Equal([3, 1, 2, 4], OrderIds(new OrderParameters("Category.Name", Desc), new OrderParameters(nameof(Product.Quantity), Asc)));
		}

		[Fact]
		public void ThreeKeys()
		{
			// inactive first: Wrench. Then active: Garden (Shovel, Rake by name desc), then Tools (Hammer).
			Assert.Equal([3, 2, 4, 1], OrderIds(
				new OrderParameters(nameof(Product.IsActive), Asc),
				new OrderParameters("Category.Name", Asc),
				new OrderParameters(nameof(Product.Name), Desc)));
		}

		// ---- computed keys (PropertySelectors functions)

		[Fact]
		public void ByCollectionCount()
		{
			// review counts: 1 → 2, 2 → 1, 3 → 0, 4 → 3
			Assert.Equal([3, 2, 1, 4], OrderIds(new OrderParameters("Reviews|" + SelectorDefaults.CountAggregateFunction, Asc)));
		}

		[Fact]
		public void ByProjectedAverage_EmptyCollectionSortsLikeNull()
		{
			// average stars: 1 → 4.5, 4 → 3.33, 2 → 3, 3 → none. SQL AVG of no rows is NULL, and SQLite puts
			// NULL last in descending order.
			Assert.Equal([1, 4, 2, 3], OrderIds(new OrderParameters("Reviews.Stars|" + SelectorDefaults.AvgAggregateFunction, Desc)));
		}

		[Fact]
		public void TypedParameters_ByCollectionCount()
		{
			PropertySelector<Product> reviewCount = "Reviews|" + SelectorDefaults.CountAggregateFunction;

			Assert.Equal([3, 2, 1, 4], OrderIds(new OrderParameters<Product>(reviewCount, Asc)));
		}

		[Fact]
		public void TypedParameters_ThenStringParameters()
		{
			// Status (typed) then review count desc (string): Draft 2; Active: Rake (3 reviews), Hammer (2); Discontinued 3
			PropertySelector<Product> status = nameof(Product.Status);

			Assert.Equal([2, 4, 1, 3], OrderIds(
				new OrderParameters<Product>(status, Asc),
				new OrderParameters("Reviews|" + SelectorDefaults.CountAggregateFunction, Desc)));
		}

		[Fact]
		public void TypedParameters_BaseStringIsTheFullSelector()
		{
			const string text = "Reviews|" + SelectorDefaults.CountAggregateFunction;
			PropertySelector<Product> selector = text;

			OrderParameters asBase = new OrderParameters<Product>(selector, Asc);

			Assert.Equal(text, asBase.Field);
		}

		// ---- provider behaviour (SQLite), recorded so a provider change is noticed

		[Fact]
		public void Nulls_SortFirstAscending_OnSqlite()
		{
			// NullableQuantity: 2 → null, 3 → 0, 1 → 100, 4 → 200. SQL Server also sorts NULL first; PostgreSQL does not.
			Assert.Equal([2, 3, 1, 4], OrderIds(new OrderParameters(nameof(Product.NullableQuantity), Asc)));
		}

		[Fact]
		public void Nulls_SortLastDescending_OnSqlite()
		{
			Assert.Equal([4, 1, 3, 2], OrderIds(new OrderParameters(nameof(Product.NullableQuantity), Desc)));
		}

		[Fact]
		public void Decimal_CannotBeOrdered_OnSqlite()
		{
			// EF Core's SQLite provider stores decimal as TEXT and refuses to ORDER BY it.
			// Works on SQL Server; listed so that the limitation is visible.
			Assert.Throws<NotSupportedException>(() => OrderIds(new OrderParameters(nameof(Product.Price), Asc)));
		}

		// ---- no-op and invalid input

		[Fact]
		public void NullParameters_LeaveTheQueryUnchanged()
		{
			using var context = Database.CreateContext();
			var query = context.Products.AsQueryable();

			Assert.Same(query, query.ApplyOrder((IEnumerable<OrderParameters>)null!));
		}

		[Fact]
		public void EmptyParameters_LeaveTheQueryUnchanged()
		{
			using var context = Database.CreateContext();
			var query = context.Products.AsQueryable();

			Assert.Same(query, query.ApplyOrder([]));
		}

		[Fact]
		public void NullSelector_LeavesTheQueryUnchanged()
		{
			using var context = Database.CreateContext();
			var query = context.Products.AsQueryable();

			Assert.Same(query, query.ApplyOrder((PropertySelector)null!, Asc));
		}

		[Fact]
		public void UnknownProperty_Throws()
		{
			Assert.ThrowsAny<Exception>(() => OrderIds(new OrderParameters("DoesNotExist", Asc)));
		}
	}
}
