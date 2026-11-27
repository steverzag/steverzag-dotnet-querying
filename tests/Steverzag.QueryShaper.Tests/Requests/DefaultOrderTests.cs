using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Pagination;
using Steverzag.QueryShaper.Tests.Configuration;
using Steverzag.QueryShaper.Tests.Infrastructure;
using Steverzag.QueryShaper.Tests.Mapping;

namespace Steverzag.QueryShaper.Tests.Requests
{
	public class ProductDefaultOrderConfiguration : QueryShaperConfiguration
	{
		protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
			.ConfigureDefaultOrder<Product>([new(nameof(Product.Id), OrderDirection.Descending)]);
	}

	/// <summary>
	/// The default order (decision 051): a request's order, then the configured default as a tie-breaker; the default
	/// alone when the request has none. Paging needs an order, so with none at all <c>ApplyPaged</c> throws.
	/// </summary>
	public class DefaultOrderTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 1 Hammer Tools | 2 Shovel Garden | 3 Wrench Tools | 4 Rake Garden

		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static QueryRequest Bind(string json) => JsonSerializer.Deserialize<QueryRequest>(json, Web)!;

		private static readonly QueryRequest NoOrder = new();

		private static readonly QueryRequest ByCategory = Bind("""{ "order": [ { "field": "Category.Name" } ] }""");

		private static IQueryShaper Shaper(bool withDefault, Action<QueryShaperOptions>? configure = null)
		{
			var services = new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>(configure);
			if (withDefault)
			{
				services.AddQueryShaper<ProductDefaultOrderConfiguration>();
			}
			return services.BuildServiceProvider().GetRequiredService<IQueryShaper>();
		}

		private int[] Ids(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			return [.. apply(context.Products).Select(p => p.Id)];
		}

		private static string OrderByClause(IQueryable query)
		{
			var sql = query.ToQueryString();
			var index = sql.LastIndexOf("ORDER BY", StringComparison.Ordinal);
			return index < 0 ? "" : sql[index..];
		}

		// ---- Apply

		[Fact]
		public void NoRequestOrder_UsesTheDefault()
		{
			Assert.Equal([4, 3, 2, 1], Ids(q => Shaper(withDefault: true).Apply(q, NoOrder)));
		}

		[Fact]
		public void ARequestOrder_GetsTheDefaultAsATieBreaker()
		{
			// Garden: 4, 2 (Id descending), then Tools: 3, 1
			Assert.Equal([4, 2, 3, 1], Ids(q => Shaper(withDefault: true).Apply(q, ByCategory)));
		}

		[Fact]
		public void ADefaultTheRequestAlreadyOrdersBy_IsNotRepeated()
		{
			using var context = Database.CreateContext();
			var query = Shaper(withDefault: true).Apply(context.Products, Bind("""{ "order": [ { "field": "Id" } ] }"""));

			Assert.Equal([1, 2, 3, 4], query.Select(p => p.Id).ToArray());
			Assert.Single(Regex.Matches(OrderByClause(query), "\"Id\""));
		}

		[Fact]
		public void WithoutADefault_ApplyLeavesTheQueryUnordered()
		{
			using var context = Database.CreateContext();
			Assert.Equal("", OrderByClause(Shaper(withDefault: false).Apply(context.Products, NoOrder)));
		}

		[Fact]
		public void ApplyOrder_AddsTheDefaultToo()
		{
			Assert.Equal([4, 2, 3, 1], Ids(q => Shaper(withDefault: true).ApplyOrder(q, ByCategory.Order)));
			Assert.Equal([4, 3, 2, 1], Ids(q => Shaper(withDefault: true).ApplyOrder(q, [])));
		}

		[Fact]
		public void TheDefault_IsntCountedAgainstMaxOrders()
		{
			Assert.Equal([4, 2, 3, 1], Ids(q => Shaper(withDefault: true, o => o.MaxOrders = 1).Apply(q, ByCategory)));
		}

		[Fact]
		public void TheDefault_IsServerConfiguration_SoTheMemberFilterDoesntApplyToIt()
		{
			var hideId = MemberFilter.All.Except((_, p) => p.Name == nameof(Product.Id));
			var shaper = Shaper(withDefault: true, o => o.MemberFilter = hideId);

			Assert.Equal([4, 3, 2, 1], Ids(q => shaper.Apply(q, NoOrder)));
			Assert.Throws<Exceptions.QueryRequestException>(() => shaper.Apply(Database.CreateContext().Products, Bind("""{ "order": [ { "field": "Id" } ] }""")));
		}

		// ---- a source the caller already ordered

		[Fact]
		public void ACallerOrderedSource_KeepsItsOrder_WhenTheRequestHasNone()
		{
			using var context = Database.CreateContext();
			var byNameDescending = context.Products.OrderByDescending(p => p.Name);

			// Wrench, Shovel, Rake, Hammer: the default isn't used
			Assert.Equal([3, 2, 4, 1], Shaper(withDefault: true).ApplyPaged(byNameDescending, NoOrder).Select(p => p.Id));
			Assert.Equal([3, 2, 4, 1], Shaper(withDefault: false).ApplyPaged(byNameDescending, NoOrder).Select(p => p.Id));
		}

		[Fact]
		public void ACallerOrderedSource_IsReplacedByARequestOrder()
		{
			using var context = Database.CreateContext();
			var byNameDescending = context.Products.OrderByDescending(p => p.Name);

			Assert.Equal([4, 2, 3, 1], Shaper(withDefault: true).ApplyPaged(byNameDescending, ByCategory).Select(p => p.Id));
		}

		[Fact]
		public void ACallerOrderBeforeAFilter_StillCounts()
		{
			using var context = Database.CreateContext();
			var request = Bind("""{ "filters": [ { "field": "Quantity", "condition": "GreaterThan", "value": 0 } ] }""");

			Assert.Equal([3, 4, 1], Shaper(withDefault: false).ApplyPaged(context.Products.OrderByDescending(p => p.Name), request).Select(p => p.Id));
		}

		// ---- paging needs an order

		[Fact]
		public void ApplyPaged_WithNoOrderAtAll_Throws()
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<InvalidOperationException>(() => Shaper(withDefault: false).ApplyPaged(context.Products, NoOrder));
			Assert.Equal(
				"Product can't be paged in a stable order: the request has no 'order', the source isn't ordered, and no default "
				+ "order is configured. Configure one with ConfigureDefaultOrder<Product>(…), or order the source.",
				ex.Message);
			Assert.Throws<InvalidOperationException>(() => Shaper(withDefault: false).ApplyPaged<Product, ProductLabel>(context.Products, NoOrder));
		}

		[Fact]
		public void ApplyPaged_WithARequestOrderButNoDefault_Works()
		{
			using var context = Database.CreateContext();
			var page = Shaper(withDefault: false).ApplyPaged(context.Products, Bind("""{ "order": [ { "field": "Id", "direction": "Descending" } ] }"""));

			Assert.Equal([4, 3, 2, 1], page.Select(p => p.Id));
		}

		[Fact]
		public void ApplyPaged_UsesTheDefault_WithAMap()
		{
			using var context = Database.CreateContext();
			var page = Shaper(withDefault: true).ApplyPaged<Product, ProductLabel>(context.Products, NoOrder);

			Assert.Equal([4, 3, 2, 1], page.Select(p => p.Id));
		}

		[Fact]
		public void APageError_IsReportedBeforeTheMissingOrder()
		{
			using var context = Database.CreateContext();
			var ex = Assert.Throws<Exceptions.QueryRequestException>(() => Shaper(withDefault: false).ApplyPaged(context.Products, Bind("""{ "page": { "index": 0 } }""")));

			Assert.Equal("page.index", ex.Path);
		}

		// ---- ApplyForPaging's fallback order (what the EF add-on passes: the primary key)

		[Fact]
		public void TheFallback_IsUsed_WhenNoDefaultIsConfigured()
		{
			IReadOnlyList<OrderParameters> fallback = [new(nameof(Product.Id))];

			Assert.Equal([1, 2, 3, 4], Ids(q => Shaper(withDefault: false).ApplyForPaging(q, NoOrder, fallback)));
			Assert.Equal([2, 4, 1, 3], Ids(q => Shaper(withDefault: false).ApplyForPaging(q, ByCategory, fallback)));
		}

		[Fact]
		public void TheDefault_WinsOverTheFallback()
		{
			IReadOnlyList<OrderParameters> fallback = [new(nameof(Product.Id))];

			Assert.Equal([4, 3, 2, 1], Ids(q => Shaper(withDefault: true).ApplyForPaging(q, NoOrder, fallback)));
		}

		// ---- the EF add-on: the primary key when nothing else orders the page

		[Fact]
		public async Task ApplyPagedAsync_FallsBackToThePrimaryKey()
		{
			using var context = Database.CreateContext();
			var page = await Shaper(withDefault: false).ApplyPagedAsync(context.Products, NoOrder);

			Assert.Equal([1, 2, 3, 4], page.Select(p => p.Id));
		}

		[Fact]
		public async Task ApplyPagedAsync_UsesThePrimaryKeyAsATieBreaker()
		{
			using var context = Database.CreateContext();
			var page = await Shaper(withDefault: false).ApplyPagedAsync<Product, ProductLabel>(context.Products, ByCategory);

			Assert.Equal([2, 4, 1, 3], page.Select(p => p.Id));
		}

		[Fact]
		public async Task ApplyPagedAsync_PrefersTheConfiguredDefault()
		{
			using var context = Database.CreateContext();
			var page = await Shaper(withDefault: true).ApplyPagedAsync(context.Products.Where(p => p.Quantity > 0), NoOrder);

			Assert.Equal([4, 3, 1], page.Select(p => p.Id));
		}

		[Fact]
		public async Task ApplyPagedAsync_OnASourceThatIsntTheEntity_Throws()
		{
			using var context = Database.CreateContext();
			var categories = context.Products.Select(p => p.Category);

			await Assert.ThrowsAsync<InvalidOperationException>(() => Shaper(withDefault: false).ApplyPagedAsync(categories, NoOrder));
		}

		// ---- configuration is checked at startup

		private class UnknownFieldConfiguration : QueryShaperConfiguration
		{
			protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
				.ConfigureDefaultOrder<Product>([new("Missing")]);
		}

		private class EmptyConfiguration : QueryShaperConfiguration
		{
			protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
				.ConfigureDefaultOrder<Product>([]);
		}

		private class NullEntryConfiguration : QueryShaperConfiguration
		{
			protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
				.ConfigureDefaultOrder<Product>([null!]);
		}

		private class ReplacedConfiguration : QueryShaperConfiguration
		{
			protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
				.ConfigureDefaultOrder<Product>([new(nameof(Product.Id), OrderDirection.Descending)])
				.ConfigureDefaultOrder<Product>([new OrderParameters<Product>(nameof(Product.Name))]);
		}

		[Fact]
		public void AnUnknownField_FailsAtStartup()
		{
			var ex = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddQueryShaper<UnknownFieldConfiguration>());
			Assert.StartsWith("Property 'Missing' was not found on type 'Product'.", ex.Message);
		}

		[Fact]
		public void AnEmptyOrNullEntry_FailsAtStartup()
		{
			Assert.Throws<ArgumentException>(() => new ServiceCollection().AddQueryShaper<EmptyConfiguration>());
			Assert.Throws<ArgumentException>(() => new ServiceCollection().AddQueryShaper<NullEntryConfiguration>());
		}

		[Fact]
		public void ConfiguringTheTypeAgain_ReplacesIt()
		{
			var shaper = new ServiceCollection().AddQueryShaper<ReplacedConfiguration>().BuildServiceProvider().GetRequiredService<IQueryShaper>();

			// Hammer, Rake, Shovel, Wrench
			Assert.Equal([1, 4, 2, 3], Ids(q => shaper.Apply(q, NoOrder)));
		}
	}
}
