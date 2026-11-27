using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Pagination;
using Steverzag.QueryShaper.Tests.Configuration;
using Steverzag.QueryShaper.Tests.Infrastructure;
using Steverzag.QueryShaper.Tests.Mapping;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary>
	/// <see cref="IQueryShaper"/>: the guarded layer over the extensions, granular and composed (decision 041.5–9).
	/// </summary>
	public class QueryShaperTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static ServiceProvider Provider(Action<QueryShaperOptions>? configure = null)
			=> new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>(configure).BuildServiceProvider();

		private static IQueryShaper Shaper(Action<QueryShaperOptions>? configure = null)
			=> Provider(configure).GetRequiredService<IQueryShaper>();

		private static QueryRequest Bind(string json) => JsonSerializer.Deserialize<QueryRequest>(json, Web)!;

		private int[] Ids(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			return [.. apply(context.Products).Select(p => p.Id)];
		}

		private string Sql(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			return apply(context.Products).ToQueryString();
		}

		private static ConditionNode Node(string field, FilterCondition condition, FilterValue? value = null)
			=> new() { Field = field, Condition = condition, Value = value };

		// Registration

		[Fact]
		public void AddQueryShaper_RegistersOneShaper_OverTheSameMapperAndSearch()
		{
			using var provider = Provider();

			Assert.Same(provider.GetRequiredService<IQueryShaper>(), provider.GetRequiredService<IQueryShaper>());
			Assert.IsAssignableFrom<IMapper>(provider.GetRequiredService<IMapper>());
			Assert.IsAssignableFrom<IBaseSearchConfiguration>(provider.GetRequiredService<IBaseSearchConfiguration>());
		}

		[Fact]
		public void AddQueryShaper_CalledTwice_ComposesTheOptions()
		{
			using var provider = new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>(o => o.MaxPageSize = 5)
				.AddQueryShaper<CategorySearchConfiguration>(o => o.DefaultPageSize = 3)
				.BuildServiceProvider();

			var shaper = provider.GetRequiredService<IQueryShaper>();

			Assert.Equal(new PageRequest(1, 3), shaper.ResolvePage(null));
			Assert.Throws<QueryRequestException>(() => shaper.ResolvePage(new PageRequest(1, 6)));
		}

		[Theory]
		[InlineData(0, 10)]
		[InlineData(10, 0)]
		[InlineData(20, 10)]
		public void InvalidOptions_FailWhenTheShaperIsResolved(int defaultSize, int maxSize)
		{
			using var provider = Provider(o => { o.DefaultPageSize = defaultSize; o.MaxPageSize = maxSize; });

			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryShaper>());
			Assert.StartsWith(nameof(QueryShaperOptions), ex.Message);
		}

		// Apply: composition

		[Fact]
		public void AnEmptyRequest_ChangesNothing()
		{
			var shaper = Shaper();
			Assert.Equal(Sql(q => q), Sql(q => shaper.Apply(q, new QueryRequest())));
		}

		[Fact]
		public void Apply_IsSearchThenFiltersThenOrder_AsTheHandChainedExtensions()
		{
			using var provider = Provider();
			var shaper = provider.GetRequiredService<IQueryShaper>();
			var search = provider.GetRequiredService<IBaseSearchConfiguration>();

			var request = Bind("""
				{ "search": "e",
				  "filters": [ { "field": "Quantity", "condition": "GreaterThan", "value": 0 },
				               { "connector": "Or", "filters": [ { "field": "CreatedAt", "condition": "LessThan", "value": "2024-02-01" } ] } ],
				  "order": [ { "field": "Category.Name" }, { "field": "Name", "direction": "Descending" } ] }
				""");

			var byHand = Sql(q => q
				.ApplyBaseFilter("e", search)
				.ApplyLogicsFilter(request.Filters)
				.ApplyOrder(request.Order));

			Assert.Equal(byHand, Sql(q => shaper.Apply(q, request)));
		}

		[Fact]
		public void Apply_RunsOnSqlite()
		{
			var request = Bind("""
				{ "search": "e",
				  "filters": [ { "field": "Quantity", "condition": "GreaterThan", "value": 0 } ],
				  "order": [ { "field": "Name" } ] }
				""");

			// Hammer, Shovel, Wrench and Rake all contain "e"; Shovel has quantity 0.
			Assert.Equal([1, 4, 3], Ids(q => Shaper().Apply(q, request)));
		}

		[Fact]
		public void Apply_WithAMap_ProjectsAfterOrdering()
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();

			var labels = shaper.Apply<Product, ProductLabel>(context.Products, Bind("""{ "order": [ { "field": "Id", "direction": "Descending" } ] }""")).ToList();

			Assert.Equal([4, 3, 2, 1], labels.Select(l => l.Id));
			Assert.Equal(["Garden", "Tools", "Garden", "Tools"], labels.Select(l => l.Name));
		}

		[Fact]
		public void ASearchOnAnUnconfiguredType_IsAServerError_NotARequestError()
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();

			Assert.Throws<InvalidOperationException>(() => shaper.Apply(context.Categories, new QueryRequest { Search = "x" }));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void ABlankSearch_IsSkipped(string? search)
		{
			var shaper = Shaper();

			Assert.Equal(Sql(q => q), Sql(q => shaper.Apply(q, new QueryRequest { Search = search })));
		}

		// Granular methods: the same SQL as their part of Apply

		[Fact]
		public void ApplySearch_IsTheConfiguredBaseSearch()
		{
			using var provider = Provider();
			var search = provider.GetRequiredService<IBaseSearchConfiguration>();
			var shaper = provider.GetRequiredService<IQueryShaper>();

			Assert.Equal(Sql(q => q.ApplyBaseFilter("ake", search)), Sql(q => shaper.ApplySearch(q, "ake")));
			Assert.Equal([4], Ids(q => shaper.ApplySearch(q, "ake")));
		}

		[Fact]
		public void ApplyFilters_IsTheFilterExtension()
		{
			var shaper = Shaper();
			FilterNode[] filters = [Node(nameof(Product.Name), FilterCondition.StartsWith, "S"), Node(nameof(Product.Id), FilterCondition.Equals, 4) with { Connector = LogicalConnector.Or }];

			Assert.Equal(Sql(q => q.ApplyLogicsFilter(filters)), Sql(q => shaper.ApplyFilters(q, filters)));
			Assert.Equal([2, 4], Ids(q => shaper.ApplyFilters(q, filters).OrderBy(p => p.Id)));
		}

		[Fact]
		public void ApplyOrder_IsTheOrderExtension()
		{
			var shaper = Shaper();
			OrderParameters[] order = [new("Category.Name"), new(nameof(Product.Quantity), OrderDirection.Descending)];

			Assert.Equal(Sql(q => q.ApplyOrder(order)), Sql(q => shaper.ApplyOrder(q, order)));
		}

		[Fact]
		public void ApplyOrder_AcceptsTypedOrderParameters()
		{
			var shaper = Shaper();
			PropertySelector<Product> reviews = "Reviews|" + SelectorDefaults.CountAggregateFunction;

			Assert.Equal([3, 2, 1, 4], Ids(q => shaper.ApplyOrder(q, [new OrderParameters<Product>(reviews)])));
		}

		[Fact]
		public void EmptyListsAndBlankSearch_ReturnTheSourceItself()
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();
			IQueryable<Product> source = context.Products;

			Assert.Same(source, shaper.ApplyFilters(source, []));
			Assert.Same(source, shaper.ApplyOrder(source, []));
			Assert.Same(source, shaper.ApplySearch(source, " "));
		}

		[Fact]
		public void Map_IsTheMapper()
		{
			using var provider = Provider();
			var mapper = provider.GetRequiredService<IMapper>();
			var shaper = provider.GetRequiredService<IQueryShaper>();
			using var context = Database.CreateContext();

			Assert.Equal(
				context.Products.ApplySelect<Product, ProductLabel>(mapper).ToQueryString(),
				shaper.Map<Product, ProductLabel>(context.Products).ToQueryString());
		}

		// Request errors: one exception type, with a path relative to what the method received

		[Theory]
		[InlineData("""{ "filters": [ { "field": "Nope", "condition": "Equals", "value": 1 } ] }""", "filters[0].field")]
		[InlineData("""{ "filters": [ { "field": "Name", "condition": "IsEmpty" }, { "filters": [ { "field": "Name", "condition": "IsEmpty" }, { "field": "Category.Nope", "condition": "IsEmpty" } ] } ] }""", "filters[1].filters[1].field")]
		[InlineData("""{ "filters": [ { "field": "Name|$FN_MATH_SQRT", "condition": "Equals", "value": 1 } ] }""", "filters[0].field")]
		[InlineData("""{ "filters": [ { "field": "CreatedAt", "condition": "Equals", "value": "yesterday" } ] }""", "filters[0].value")]
		[InlineData("""{ "filters": [ { "field": "Quantity", "condition": "Equals", "value": 1.5 } ] }""", "filters[0].value")]
		[InlineData("""{ "filters": [ { "field": "Quantity", "condition": "Equals", "value": "1" } ] }""", "filters[0].value")]
		[InlineData("""{ "filters": [ { "field": "Quantity", "condition": "Equals" } ] }""", "filters[0].value")]
		[InlineData("""{ "filters": [ { "field": "Status", "condition": "Equals", "value": "Unknown" } ] }""", "filters[0].value")]
		[InlineData("""{ "filters": [ { "field": "Quantity", "condition": "In", "value": [] } ] }""", "filters[0].value")]
		[InlineData("""{ "filters": [ { "field": "Quantity", "condition": "StartsWith", "value": 1 } ] }""", "filters[0].condition")]
		[InlineData("""{ "filters": [ { "field": "Reviews", "condition": "Equals", "value": 1 } ] }""", "filters[0].condition")]
		[InlineData("""{ "order": [ { "field": "Name" }, { "field": "Nope" } ] }""", "order[1].field")]
		[InlineData("""{ "order": [ { "field": "Name +" } ] }""", "order[0].field")]
		public void Apply_ReportsWhereTheRequestIsWrong(string json, string path)
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => shaper.Apply(context.Products, Bind(json)));

			Assert.Equal(path, ex.Path);
			Assert.Equal($"{path}: {ex.Detail}", ex.Message);
			Assert.NotNull(ex.InnerException);
			Assert.Equal(ex.InnerException.Message, ex.Detail);
		}

		[Fact]
		public void AnUnknownField_IsReportedAtItsPositionInTheWholeSelector()
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();

			const string selector = "CreatedAt|$FN_DT_ADDDAYS(3)|Missing";
			var ex = Assert.Throws<QueryRequestException>(() => shaper.ApplyFilters(context.Products, [Node(selector, FilterCondition.IsEmpty)]));

			// The extensions' non-generic build reports this at 0 in 'Missing' (roadmap, decision 022); re-parsing
			// against the source type gives the position in the whole selector.
			var inner = Assert.IsType<SelectorParseException>(ex.InnerException);
			Assert.Equal(selector.IndexOf("Missing"), inner.Position);
		}

		[Fact]
		public void TheGranularMethods_ReportPathsRelativeToTheirInput()
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();

			FilterNode[] filters = [Node(nameof(Product.Name), FilterCondition.IsEmpty), new GroupNode { Filters = [Node("Nope", FilterCondition.IsEmpty)] }];
			Assert.Equal("[1].filters[0].field", Assert.Throws<QueryRequestException>(() => shaper.ApplyFilters(context.Products, filters)).Path);
			Assert.Equal("[0].field", Assert.Throws<QueryRequestException>(() => shaper.ApplyOrder(context.Products, [new OrderParameters("Nope")])).Path);
			Assert.Equal("size", Assert.Throws<QueryRequestException>(() => shaper.ResolvePage(new PageRequest(1, 0))).Path);
		}

		[Fact]
		public void NullEntries_AreRequestErrors()
		{
			var shaper = Shaper();
			using var context = Database.CreateContext();

			Assert.Equal("[0]", Assert.Throws<QueryRequestException>(() => shaper.ApplyFilters(context.Products, [null!])).Path);
			Assert.Equal("[1]", Assert.Throws<QueryRequestException>(() => shaper.ApplyOrder(context.Products, [new OrderParameters("Name"), null!])).Path);
		}

		[Fact]
		public void TheExtensions_StillThrowTheirOwnExceptions()
		{
			using var context = Database.CreateContext();

			Assert.ThrowsAny<SelectorParseException>(() => context.Products.ApplyLogicsFilter([Node("Nope", FilterCondition.IsEmpty)]));
			Assert.Throws<FilterNotSupportedException>(() => context.Products.ApplyLogicsFilter([Node(nameof(Product.Quantity), FilterCondition.StartsWith, 1)]));
		}

		// Paging

		[Fact]
		public void ResolvePage_FillsTheDefaults()
		{
			var shaper = Shaper();

			Assert.Equal(new PageRequest(1, 10), shaper.ResolvePage(null));
			Assert.Equal(new PageRequest(3, 10), shaper.ResolvePage(new PageRequest(3)));
			Assert.Equal(new PageRequest(2, 100), shaper.ResolvePage(new PageRequest(2, 100)));
		}

		[Theory]
		[InlineData(0, 10, "index", "The page index must be at least 1, not 0.")]
		[InlineData(-1, null, "index", "The page index must be at least 1, not -1.")]
		[InlineData(1, 0, "size", "The page size must be at least 1, not 0.")]
		[InlineData(1, -5, "size", "The page size must be at least 1, not -5.")]
		[InlineData(1, 101, "size", "The page size can't be more than 100, not 101.")]
		public void ResolvePage_RejectsWhatItWontServe(int index, int? size, string path, string detail)
		{
			var ex = Assert.Throws<QueryRequestException>(() => Shaper().ResolvePage(new PageRequest(index, size)));

			Assert.Equal(path, ex.Path);
			Assert.Equal(detail, ex.Detail);
		}

		[Fact]
		public void ApplyPaged_UsesTheDefaultPage_WhenTheRequestHasNone()
		{
			var page = Shaper(o => o.DefaultPageSize = 3).ApplyPaged(Database.CreateContext().Products, Bind("""{ "order": [ { "field": "Id" } ] }"""));

			Assert.Equal([1, 2, 3], page.Select(p => p.Id));
			Assert.Equal(1, page.PageIndex);
			Assert.Equal(3, page.PageSize);
			Assert.Equal(4, page.TotalCount);
		}

		[Fact]
		public void ApplyPaged_ReportsThePageAtItsPathInTheRequest()
		{
			var ex = Assert.Throws<QueryRequestException>(() => Shaper().ApplyPaged(Database.CreateContext().Products, Bind("""{ "page": { "index": 1, "size": 500 } }""")));

			Assert.Equal("page.size", ex.Path);
		}

		[Fact]
		public void ApplyPaged_WithAMap_PagesTheProjection()
		{
			var page = Shaper().ApplyPaged<Product, ProductLabel>(Database.CreateContext().Products, Bind("""{ "order": [ { "field": "Id" } ], "page": { "index": 2, "size": 3 } }"""));

			Assert.Equal([4], page.Select(l => l.Id));
			Assert.Equal(2, page.TotalPages);
		}

		[Fact]
		public async Task ApplyPagedAsync_RunsOnEfCore()
		{
			using var context = Database.CreateContext();

			var page = await Shaper().ApplyPagedAsync(context.Products, Bind("""
				{ "filters": [ { "field": "Quantity", "condition": "GreaterThan", "value": 0 } ],
				  "order": [ { "field": "Id", "direction": "Descending" } ],
				  "page": { "index": 1, "size": 2 } }
				"""));

			Assert.Equal([4, 3], page.Select(p => p.Id));
			Assert.Equal(3, page.TotalCount);
		}

		[Fact]
		public async Task ApplyPagedAsync_WithAMap_AndAPathForThePage()
		{
			using var context = Database.CreateContext();
			var shaper = Shaper();

			var page = await shaper.ApplyPagedAsync<Product, ProductLabel>(context.Products, Bind("""{ "order": [ { "field": "Id" } ], "page": { "index": 1, "size": 1 } }"""));
			Assert.Equal("Tools", Assert.Single(page).Name);

			var ex = await Assert.ThrowsAsync<QueryRequestException>(() => shaper.ApplyPagedAsync(context.Products, Bind("""{ "page": { "index": 0 } }""")));
			Assert.Equal("page.index", ex.Path);
		}

		[Fact]
		public void Paginate_TakesAResolvedPage()
		{
			using var context = Database.CreateContext();

			Assert.Equal(2, context.Products.OrderBy(p => p.Id).Paginate(new PageRequest(1, 2)).Count);
			var ex = Assert.Throws<ArgumentException>(() => context.Products.Paginate(new PageRequest(1)));
			Assert.Contains(nameof(IQueryShaper.ResolvePage), ex.Message);
		}
	}
}
