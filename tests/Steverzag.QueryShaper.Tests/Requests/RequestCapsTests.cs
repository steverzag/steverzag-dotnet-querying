using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Tests.Configuration;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary><see cref="QueryShaperOptions.MaxOrders"/>, <see cref="QueryShaperOptions.MaxSearchLength"/> and <see cref="QueryShaperOptions.MaxListValues"/>.</summary>
	public class RequestCapsTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static ServiceProvider Provider(Action<QueryShaperOptions>? configure = null)
			=> new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>(configure).BuildServiceProvider();

		private static IQueryShaper Shaper(Action<QueryShaperOptions>? configure = null) => Provider(configure).GetRequiredService<IQueryShaper>();

		private static QueryRequest Bind(string json) => JsonSerializer.Deserialize<QueryRequest>(json, Web)!;

		private static string Orders(int count)
			=> $$"""{ "order": [ {{string.Join(", ", Enumerable.Repeat("""{ "field": "Id" }""", count))}} ] }""";

		private static string InList(int count, string field = "Id", string condition = "In")
			=> $$"""{ "filters": [ { "field": "{{field}}", "condition": "{{condition}}", "value": [ {{string.Join(", ", Enumerable.Range(1, count))}} ] } ] }""";

		private int[] Ids(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			return [.. apply(context.Products).Select(p => p.Id)];
		}

		private QueryRequestException Rejects(IQueryShaper shaper, string json)
		{
			using var context = Database.CreateContext();
			return Assert.Throws<QueryRequestException>(() => shaper.Apply(context.Products, Bind(json)));
		}

		[Fact]
		public void TheDefaults()
		{
			var options = new QueryShaperOptions();
			Assert.Equal(10, options.MaxOrders);
			Assert.Equal(200, options.MaxSearchLength);
			Assert.Equal(100, options.MaxListValues);
		}

		// Orders

		[Fact]
		public void Orders_AtTheLimit_Run()
		{
			var shaper = Shaper(o => o.MaxOrders = 3);
			Assert.Equal([1, 2, 3, 4], Ids(q => shaper.Apply(q, Bind(Orders(3)))));
		}

		[Fact]
		public void Orders_OverTheLimit_AreReportedAtOrder()
		{
			var ex = Rejects(Shaper(o => o.MaxOrders = 3), Orders(4));

			Assert.Equal("order", ex.Path);
			Assert.Equal("A request can have at most 3 orders, not 4.", ex.Detail);
		}

		[Fact]
		public void Orders_TheDefaultLimit_Applies()
		{
			_ = Ids(q => Shaper().Apply(q, Bind(Orders(10))));
			Assert.Equal("A request can have at most 10 orders, not 11.", Rejects(Shaper(), Orders(11)).Detail);
		}

		[Fact]
		public void Orders_TheCountIsCheckedBeforeTheFields()
		{
			using var context = Database.CreateContext();
			OrderParameters[] order = [new("Nope"), new("Id"), new("Id")];

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(o => o.MaxOrders = 2).ApplyOrder(context.Products, order));

			Assert.Equal("", ex.Path);
			Assert.Equal("A request can have at most 2 orders, not 3.", ex.Detail);
		}

		// Search

		[Fact]
		public void Search_AtTheLimit_Runs()
		{
			var shaper = Shaper(o => o.MaxSearchLength = 3);
			Assert.Empty(Ids(q => shaper.Apply(q, new QueryRequest { Search = "xyz" })));
		}

		[Fact]
		public void Search_OverTheLimit_IsReportedAtSearch()
		{
			var ex = Rejects(Shaper(o => o.MaxSearchLength = 3), """{ "search": "abcd" }""");

			Assert.Equal("search", ex.Path);
			Assert.Equal("The search can be at most 3 characters long, not 4.", ex.Detail);
		}

		[Fact]
		public void Search_TheDefaultLimit_Applies()
		{
			_ = Ids(q => Shaper().Apply(q, new QueryRequest { Search = new string('e', 200) }));
			var ex = Rejects(Shaper(), JsonSerializer.Serialize(new { search = new string('e', 201) }));
			Assert.Equal("The search can be at most 200 characters long, not 201.", ex.Detail);
		}

		[Fact]
		public void Search_ABlankSearch_IsCheckedToo()
		{
			// Whitespace is ignored as a search, but a client can't send megabytes of it either.
			var ex = Rejects(Shaper(o => o.MaxSearchLength = 3), """{ "search": "    " }""");
			Assert.Equal("search", ex.Path);
		}

		[Fact]
		public void Search_TheGranularMethod_ReportsAnEmptyPath()
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(o => o.MaxSearchLength = 3).ApplySearch(context.Products, "abcd"));

			Assert.Equal("", ex.Path);
		}

		// List values

		[Theory]
		[InlineData("Id", "In")]
		[InlineData("Id", "NotIn")]
		[InlineData("Measurements", "ContainsAny")]
		[InlineData("Measurements", "ContainsAll")]
		public void AList_AtTheLimit_Runs(string field, string condition)
		{
			var shaper = Shaper(o => o.MaxListValues = 3);
			_ = Ids(q => shaper.Apply(q, Bind(InList(3, field, condition))));
		}

		[Theory]
		[InlineData("Id", "In")]
		[InlineData("Id", "NotIn")]
		[InlineData("Measurements", "ContainsAny")]
		[InlineData("Measurements", "ContainsAll")]
		public void AList_OverTheLimit_IsReportedAtItsValue(string field, string condition)
		{
			var ex = Rejects(Shaper(o => o.MaxListValues = 3), InList(4, field, condition));

			Assert.Equal("filters[0].value", ex.Path);
			Assert.Equal("A list can have at most 3 values, not 4.", ex.Detail);
		}

		[Fact]
		public void AList_InAGroup_IsReportedAtItsPath()
		{
			const string json = """
				{ "filters": [ { "field": "Name", "condition": "IsNotEmpty" },
				               { "connector": "Or", "filters": [ { "field": "Id", "condition": "In", "value": [1, 2] },
				                                                  { "field": "Id", "condition": "In", "value": [1, 2, 3] } ] } ] }
				""";

			var ex = Rejects(Shaper(o => o.MaxListValues = 2), json);

			Assert.Equal("filters[1].filters[1].value", ex.Path);
		}

		[Fact]
		public void AList_TheDefaultLimit_Applies()
		{
			_ = Ids(q => Shaper().Apply(q, Bind(InList(100))));
			Assert.Equal("A list can have at most 100 values, not 101.", Rejects(Shaper(), InList(101)).Detail);
		}

		[Fact]
		public void AList_IsCheckedPerCondition_NotInTotal()
		{
			const string json = """
				{ "filters": [ { "field": "Id", "condition": "In", "value": [1, 2, 3] },
				               { "field": "Id", "condition": "NotIn", "value": [4, 5, 6] } ] }
				""";

			Assert.Equal([1, 2, 3], Ids(q => Shaper(o => o.MaxListValues = 3).Apply(q, Bind(json)).OrderBy(p => p.Id)));
		}

		[Fact]
		public void AList_BuiltInCode_IsCheckedToo()
		{
			using var context = Database.CreateContext();
			FilterNode[] filters = [new ConditionNode { Field = "Id", Condition = FilterCondition.In, Value = new List<object> { 1, 2, 3 } }];

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(o => o.MaxListValues = 2).ApplyFilters(context.Products, filters));

			Assert.Equal("[0].value", ex.Path);
		}

		[Fact]
		public void TheTrustedExtensions_DontCheck()
		{
			using var context = Database.CreateContext();
			Assert.Equal([1, 2, 3, 4], Ids(q => q.ApplyLogicsFilter(Bind(InList(500)).Filters).OrderBy(p => p.Id)));
		}

		// Options

		[Theory]
		[InlineData(nameof(QueryShaperOptions.MaxOrders))]
		[InlineData(nameof(QueryShaperOptions.MaxSearchLength))]
		[InlineData(nameof(QueryShaperOptions.MaxListValues))]
		public void AMaximumBelowOne_FailsWhenTheShaperIsResolved(string option)
		{
			using var provider = Provider(o =>
			{
				switch (option)
				{
					case nameof(QueryShaperOptions.MaxOrders): o.MaxOrders = 0; break;
					case nameof(QueryShaperOptions.MaxSearchLength): o.MaxSearchLength = 0; break;
					default: o.MaxListValues = 0; break;
				}
			});

			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryShaper>());
			Assert.Equal($"QueryShaperOptions: {option} must be at least 1.", ex.Message);
		}
	}
}
