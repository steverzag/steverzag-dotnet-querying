using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	/// <summary>
	/// The base ("global") search: one search text matched with Contains against several fields, OR-ed together.
	/// </summary>
	public class BaseSearchTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 1 Hammer | "Steel head"  | Tools  | Qty 100
		// 2 Shovel | null          | Garden | Qty 0
		// 3 Wrench | ""            | Tools  | Qty 50
		// 4 Rake   | "Garden rake" | Garden | Qty 200

		private int[] Ids(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			return apply(context.Products).OrderBy(p => p.Id).Select(p => p.Id).ToArray();
		}

		// ---- ad-hoc lookups: ApplyBaseFilter(lookups, value)

		[Fact]
		public void SingleLookup()
		{
			Assert.Equal([1, 4], Ids(q => q.ApplyBaseFilter([nameof(Product.Name)], "a")));
		}

		[Fact]
		public void SeveralLookups_AreOred()
		{
			// "en": Wrench (name), Garden (category of 2 and 4)
			Assert.Equal([2, 3, 4], Ids(q => q.ApplyBaseFilter([nameof(Product.Name), "Category.Name"], "en")));
		}

		[Fact]
		public void NullableLookup_NullRowsDoNotMatch()
		{
			Assert.Equal([1, 4], Ids(q => q.ApplyBaseFilter([nameof(Product.Description)], "e")));
		}

		[Fact]
		public void NonStringLookup_IsSearchedAsText()
		{
			// Quantity.ToString(): "100", "0", "50", "200"
			Assert.Equal([4], Ids(q => q.ApplyBaseFilter([nameof(Product.Quantity)], "20")));
		}

		[Fact]
		public void IsCaseSensitive_OnSqlite()
		{
			Assert.Empty(Ids(q => q.ApplyBaseFilter([nameof(Product.Name)], "hammer")));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void BlankSearch_LeavesTheQueryUnchanged(string? value)
		{
			using var context = Database.CreateContext();
			var query = context.Products.AsQueryable();

			Assert.Same(query, query.ApplyBaseFilter([nameof(Product.Name)], value));
		}

		[Fact]
		public void NoLookups_LeavesTheQueryUnchanged()
		{
			using var context = Database.CreateContext();
			var query = context.Products.AsQueryable();

			Assert.Same(query, query.ApplyBaseFilter([], "a"));
		}

		[Fact]
		public void NullLookups_Throw()
		{
			using var context = Database.CreateContext();
			Assert.Throws<ArgumentNullException>(() => context.Products.ApplyBaseFilter((IEnumerable<string>)null!, "a"));
		}

		// ---- configured search: BaseSearchConfiguration + ApplyBaseFilter(value, configuration)

		[Fact]
		public void Configured_FromLookups()
		{
			var configuration = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name), "Category.Name"]);

			Assert.Equal([2, 3, 4], Ids(q => q.ApplyBaseFilter("en", configuration)));
		}

		[Fact]
		public void Configured_SearchValueIsSubstitutedPerCall()
		{
			// One configuration, two searches: the search text is a parameter, not baked in at configuration time.
			var configuration = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name)]);

			Assert.Equal([2], Ids(q => q.ApplyBaseFilter("Sh", configuration)));
			Assert.Equal([3], Ids(q => q.ApplyBaseFilter("Wr", configuration)));
		}

		[Fact]
		public void Configured_FromACustomPredicate()
		{
			var configuration = new BaseSearchConfiguration()
				.Configure<Product>((p, search) => p.Name.StartsWith(search) || p.Category.Name == search);

			Assert.Equal([1, 2, 4], Ids(q => q.ApplyBaseFilter("Garden", configuration).Union(q.ApplyBaseFilter("H", configuration))));
		}

		[Fact]
		public void Configured_ConfiguringTheTypeAgain_ReplacesIt()
		{
			var configuration = new BaseSearchConfiguration()
				.Configure<Product>([nameof(Product.Name)])
				.Configure<Product>(["Category.Name"]);

			Assert.Equal([1, 3], Ids(q => q.ApplyBaseFilter("Tools", configuration)));
		}

		[Fact]
		public void Configured_TypeWithoutConfiguration_Throws()
		{
			// Decision 010.8. This used to return every row, which hid the missing configuration.
			var configuration = new BaseSearchConfiguration().Configure<Category>([nameof(Category.Name)]);

			var exception = Assert.Throws<InvalidOperationException>(() => Ids(q => q.ApplyBaseFilter("zzz", configuration)));
			Assert.Contains(nameof(Product), exception.Message);
			Assert.Contains("ConfigureSearch<Product>", exception.Message);
		}

		[Fact]
		public void Configured_TypeWithoutConfiguration_ThrowsBeforeTheQueryRuns()
		{
			using var context = Database.CreateContext();
			var configuration = new BaseSearchConfiguration();

			Assert.Throws<InvalidOperationException>(() => context.Products.ApplyBaseFilter("zzz", configuration));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Configured_TypeWithoutConfiguration_BlankSearch_IsNotFiltered(string? value)
		{
			// A blank search means "no search", so it doesn't need a configuration.
			var configuration = new BaseSearchConfiguration().Configure<Category>([nameof(Category.Name)]);

			Assert.Equal([1, 2, 3, 4], Ids(q => q.ApplyBaseFilter(value, configuration)));
		}

		[Fact]
		public void Configured_BlankSearch_IsNotFiltered()
		{
			var configuration = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name)]);

			Assert.Equal([1, 2, 3, 4], Ids(q => q.ApplyBaseFilter(" ", configuration)));
		}

		[Fact]
		public void Configured_GetSearchPredicate_ForAnUnconfiguredType_Throws()
		{
			Assert.Throws<InvalidOperationException>(() => new BaseSearchConfiguration().GetSearchPredicate<Product>("a"));
		}

		[Fact]
		public void Configured_NoLookups_Throws()
		{
			Assert.Throws<ArgumentException>(() => new BaseSearchConfiguration().Configure<Product>([]));
		}

		// ---- combined: ApplyFilters(lookups, value, filters)

		[Fact]
		public void ApplyFilters_AndsTheSearchWithTheLogicFilters()
		{
			// search "en" → 2, 3, 4. And quantity > 10 → 3, 4.
			Assert.Equal([3, 4], Ids(q => q.ApplyFilters(
				[nameof(Product.Name), "Category.Name"], "en",
				[new ConditionNode { FieldSelector = nameof(Product.Quantity), FilterCondition = FilterCondition.GreaterThan, SearchValue = 10 }])));
		}

		[Fact]
		public void ApplyFilters_WithoutLookups_OnlyAppliesTheLogicFilters()
		{
			Assert.Equal([1, 4], Ids(q => q.ApplyFilters(
				null, "ignored",
				[new ConditionNode { FieldSelector = nameof(Product.Quantity), FilterCondition = FilterCondition.GreaterThan, SearchValue = 50 }])));
		}
	}
}
