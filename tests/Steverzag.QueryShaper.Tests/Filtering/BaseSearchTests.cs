using Microsoft.Extensions.DependencyInjection;
using Steverzag.QueryShaper.Configuration;
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

		// ---- lookups must be strings (decision 040): a non-string used to be searched through ToString(),
		// whose text depends on the provider ("20" matched Quantity 200)

		public static TheoryData<string, string> NonStringLookups => new()
		{
			{ nameof(Product.Quantity), "int" },
			{ nameof(Product.NullableQuantity), "int?" },
			{ nameof(Product.Price), "decimal" },
			{ nameof(Product.CreatedAt), "DateTime" },
			{ nameof(Product.DeletedAt), "DateTime?" },
			{ nameof(Product.ReleaseDate), "DateOnly" },
			{ nameof(Product.Code), "Guid" },
			{ nameof(Product.IsActive), "bool" },
			{ nameof(Product.Status), "ProductStatus" },
			{ nameof(Product.Grade), "char" },
			{ nameof(Product.Tags), "List<string>" },
			{ "Reviews.Stars", "IEnumerable<int>" },
			{ "Name|$FN_STR_LENGTH", "int" },
		};

		[Theory]
		[MemberData(nameof(NonStringLookups))]
		public void NonStringLookup_IsRejected_AdHoc(string lookup, string typeName)
		{
			using var context = Database.CreateContext();

			var exception = Assert.Throws<ArgumentException>(() => context.Products.ApplyBaseFilter([lookup], "20"));
			Assert.Equal("searchLookups", exception.ParamName);
			Assert.StartsWith($"Search lookups must be strings: '{lookup}' is {typeName} on Product.", exception.Message);
		}

		[Theory]
		[MemberData(nameof(NonStringLookups))]
		public void NonStringLookup_IsRejected_WhenConfigured(string lookup, string typeName)
		{
			var exception = Assert.Throws<ArgumentException>(() => new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name), lookup]));
			Assert.StartsWith($"Search lookups must be strings: '{lookup}' is {typeName} on Product.", exception.Message);
		}

		private class QuantitySearchConfiguration : QueryShaperConfiguration
		{
			protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
				.ConfigureSearch<Product>([nameof(Product.Quantity)]);
		}

		[Fact]
		public void NonStringLookup_IsRejected_ByAddQueryShaper()
		{
			// ConfigureSearch runs inside AddQueryShaper, so a bad lookup fails at startup
			var exception = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddQueryShaper<QuantitySearchConfiguration>());
			Assert.StartsWith("Search lookups must be strings: 'Quantity' is int on Product.", exception.Message);
		}

		[Fact]
		public void SelectorProducingAString_IsAccepted()
		{
			// Description is null for 2: COALESCE turns it into "", which doesn't contain "e"
			const string lookup = "Description|$FN_COALESCE(\"\")";
			var configuration = new BaseSearchConfiguration().Configure<Product>([lookup]);

			Assert.Equal([1, 4], Ids(q => q.ApplyBaseFilter([lookup], "e")));
			Assert.Equal([1, 4], Ids(q => q.ApplyBaseFilter("e", configuration)));
		}

		[Fact]
		public void StringFunctionLookup_IsAccepted()
		{
			// Name lowered: "hammer", "shovel", "wrench", "rake"
			Assert.Equal([1], Ids(q => q.ApplyBaseFilter(["Name|$FN_STR_TOLOWER"], "ham")));
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
				[new ConditionNode { FieldSelector = nameof(Product.Quantity), FilterCondition = FilterCondition.GreaterThan, Value = 10 }])));
		}

		[Fact]
		public void ApplyFilters_WithoutLookups_OnlyAppliesTheLogicFilters()
		{
			Assert.Equal([1, 4], Ids(q => q.ApplyFilters(
				null, "ignored",
				[new ConditionNode { FieldSelector = nameof(Product.Quantity), FilterCondition = FilterCondition.GreaterThan, Value = 50 }])));
		}
	}
}
