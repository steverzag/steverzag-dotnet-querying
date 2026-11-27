using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Tests.Configuration;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary><see cref="QueryShaperOptions.MemberFilter"/>: client selectors that reach a hidden member are rejected with their path.</summary>
	public class MemberFilterRequestTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static readonly MemberFilter NoPrice = MemberFilter.Portable.Hide<Product>(p => p.Price);

		private static IQueryShaper Shaper(MemberFilter? filter = null)
			=> new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>(o => { if (filter is not null) o.MemberFilter = filter; })
				.BuildServiceProvider()
				.GetRequiredService<IQueryShaper>();

		private static QueryRequest Bind(string json) => JsonSerializer.Deserialize<QueryRequest>(json, Web)!;

		private string Sql(IQueryShaper shaper, string json)
		{
			using var context = Database.CreateContext();
			return shaper.Apply(context.Products, Bind(json)).ToQueryString();
		}

		[Fact]
		public void TheDefault_IsAll()
		{
			Assert.Same(MemberFilter.All, new QueryShaperOptions().MemberFilter);
			using var context = Database.CreateContext();
			var query = Shaper().Apply(context.Products, Bind("""{ "filters": [ { "field": "OpensAt.Ticks", "condition": "GreaterThan", "value": 0 } ] }"""));

			Assert.Throws<InvalidOperationException>(() => query.ToQueryString());
		}

		[Theory]
		[InlineData("""{ "filters": [ { "field": "CreatedAt.Ticks", "condition": "GreaterThan", "value": 0 } ] }""", "filters[0].field", 10)]
		[InlineData("""{ "filters": [ { "field": "DeletedAt.TimeOfDay", "condition": "IsEmpty" } ] }""", "filters[0].field", 10)]
		[InlineData("""{ "filters": [ { "field": "Name", "condition": "IsEmpty" }, { "connector": "Or", "filters": [ { "field": "OpensAt.Hour", "condition": "Equals", "value": 8 } ] } ] }""", "filters[1].filters[0].field", 8)]
		[InlineData("""{ "filters": [ { "field": "$FN_MATH_MAX(CreatedAt.Ticks, 1L)", "condition": "GreaterThan", "value": 0 } ] }""", "filters[0].field", 23)]
		[InlineData("""{ "order": [ { "field": "Name" }, { "field": "CreatedAt.Second" } ] }""", "order[1].field", 10)]
		[InlineData("""{ "filters": [ { "field": "Price", "condition": "GreaterThan", "value": 1 } ] }""", "filters[0].field", 0)]
		[InlineData("""{ "filters": [ { "field": "Quantity + Price", "condition": "GreaterThan", "value": 1 } ] }""", "filters[0].field", 11)]
		[InlineData("""{ "order": [ { "field": "Price" } ] }""", "order[0].field", 0)]
		public void AHiddenMember_IsReportedAtItsPath_AndItsPosition(string json, string path, int position)
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(NoPrice).Apply(context.Products, Bind(json)));

			Assert.Equal(path, ex.Path);
			var inner = Assert.IsType<SelectorParseException>(ex.InnerException);
			Assert.Equal(position, inner.Position);
			Assert.Contains("was not found on type", inner.Detail);
			Assert.EndsWith("is not allowed.", Assert.IsType<SelectorParseException>(inner.InnerException).Detail);
		}

		[Theory]
		[InlineData("Price", "Property 'Price' was not found on type 'Product'.")]
		[InlineData("price", "Property 'price' was not found on type 'Product'.")]
		[InlineData("Nope", "Property 'Nope' was not found on type 'Product'.")]
		public void AHiddenMember_ReachesTheClientLikeAMissingOne(string field, string detail)
		{
			using var context = Database.CreateContext();
			var json = $$"""{ "order": [ { "field": "{{field}}" } ] }""";

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(NoPrice).Apply(context.Products, Bind(json)));

			Assert.Equal("order[0].field", ex.Path);
			Assert.Equal($"{detail} Position 0 in '{field}'.", ex.Detail);
		}

		[Fact]
		public void TheGranularMethods_ApplyTheFilterToo()
		{
			var shaper = Shaper(NoPrice);
			using var context = Database.CreateContext();

			ConditionNode node = new() { Field = "CreatedAt.Ticks", Condition = FilterCondition.GreaterThan, Value = 0 };
			Assert.Equal("[0].field", Assert.Throws<QueryRequestException>(() => shaper.ApplyFilters(context.Products, [node])).Path);
			Assert.Equal("[0].field", Assert.Throws<QueryRequestException>(() => shaper.ApplyOrder(context.Products, [new OrderParameters("Price")])).Path);
			Assert.Equal("[0].field", Assert.Throws<QueryRequestException>(() => shaper.ApplyOrder(context.Products, [new OrderParameters<Product>("CreatedAt.Ticks")])).Path);
		}

		[Fact]
		public void AllowedSelectors_BuildTheSameQuery_AsWithoutAFilter()
		{
			const string json = """
				{
				  "search": "a",
				  "filters": [
				    { "field": "CreatedAt.Year", "condition": "Equals", "value": 2024 },
				    { "connector": "Or", "filters": [ { "field": "ReleaseDate.DayOfYear", "condition": "GreaterThan", "value": 10 } ] }
				  ],
				  "order": [ { "field": "CreatedAt.Date", "direction": "Descending" }, { "field": "Name" } ]
				}
				""";

			Assert.Equal(Sql(Shaper(), json), Sql(Shaper(NoPrice), json));
			Assert.Equal(Sql(Shaper(), json), Sql(Shaper(MemberFilter.Portable), json));
		}

		[Fact]
		public void TheConfiguredSearch_IsNotFiltered()
		{
			var shaper = Shaper(MemberFilter.All.Hide<Product>(p => p.Name));

			Assert.Contains("\"p\".\"Name\"", Sql(shaper, """{ "search": "a" }"""));
		}

		[Fact]
		public void ANullFilter_FailsWhenTheShaperIsResolved()
		{
			var provider = new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>(o => o.MemberFilter = null!)
				.BuildServiceProvider();

			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryShaper>());
			Assert.Contains(nameof(QueryShaperOptions.MemberFilter), ex.Message);
		}
	}
}
