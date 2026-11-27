using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.EntityFrameworkCore;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Tests.Configuration;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary><see cref="QueryShaperOptions.UnsortableTypes"/>: an order on a type the database can't sort is a request error, not a failure when the query runs.</summary>
	public class OrderGuardTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static IQueryShaper Shaper(IEnumerable<Type>? unsortable = null)
			=> new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>(o => { if (unsortable is not null) o.UnsortableTypes.UnionWith(unsortable); })
				.BuildServiceProvider()
				.GetRequiredService<IQueryShaper>();

		private static QueryRequest Bind(string json) => JsonSerializer.Deserialize<QueryRequest>(json, Web)!;

		[Fact]
		public void TheDefault_IsEmpty_SoTheDatabaseFailsWhenTheQueryRuns()
		{
			Assert.Empty(new QueryShaperOptions().UnsortableTypes);
			using var context = Database.CreateContext();

			var query = Shaper().Apply(context.Products, Bind("""{ "order": [ { "field": "Price" } ] }"""));

			Assert.Throws<NotSupportedException>(() => query.ToList());
		}

		[Theory]
		[InlineData("Price", "decimal")]
		[InlineData("Warranty", "TimeSpan")]
		[InlineData("Price|$FN_MATH_ROUND", "decimal")]
		[InlineData("$FN_MATH_MAX(Price, 1)", "decimal")]
		public void AnUnsortableType_IsReportedAtItsPath(string field, string type)
		{
			using var context = Database.CreateContext();
			var json = $$"""{ "order": [ { "field": "Name" }, { "field": {{JsonSerializer.Serialize(field)}} } ] }""";

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(ProviderUnsortableTypes.Sqlite).Apply(context.Products, Bind(json)));

			Assert.Equal("order[1].field", ex.Path);
			Assert.Equal($"'{field}' can't be ordered: the database can't sort {type} values.", ex.Detail);
		}

		[Fact]
		public void ANullableValue_IsCheckedByItsUnderlyingType()
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => Shaper([typeof(int)]).ApplyOrder(context.Products, [new OrderParameters(nameof(Product.NullableQuantity))]));

			Assert.Equal("[0].field", ex.Path);
		}

		[Fact]
		public void TypedOrders_AreCheckedToo()
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(ProviderUnsortableTypes.Sqlite).ApplyOrder(context.Products, [new OrderParameters<Product>(nameof(Product.Price))]));

			Assert.Equal("[0].field", ex.Path);
		}

		[Fact]
		public void SortableFields_RunAsBefore()
		{
			using var context = Database.CreateContext();
			const string json = """{ "order": [ { "field": "CreatedAt", "direction": "Descending" }, { "field": "Quantity" }, { "field": "Name" } ] }""";

			var guarded = Shaper(ProviderUnsortableTypes.Sqlite).Apply(context.Products, Bind(json));
			var unguarded = Shaper().Apply(context.Products, Bind(json));

			Assert.Equal(unguarded.ToQueryString(), guarded.ToQueryString());
			Assert.Equal(unguarded.Select(p => p.Id).ToList(), guarded.Select(p => p.Id).ToList());
		}

		[Fact]
		public void FiltersOnAnUnsortableType_AreNotAffected()
		{
			using var context = Database.CreateContext();

			var query = Shaper(ProviderUnsortableTypes.Sqlite).Apply(context.Products, Bind("""{ "filters": [ { "field": "Price", "condition": "GreaterThan", "value": 1 } ] }"""));

			Assert.NotEmpty(query.ToList());
		}

		[Fact]
		public void ANullSet_FailsWhenTheShaperIsResolved()
		{
			var provider = new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>(o => o.UnsortableTypes = null!)
				.BuildServiceProvider();

			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryShaper>());
			Assert.Contains(nameof(QueryShaperOptions.UnsortableTypes), ex.Message);
		}
	}
}
