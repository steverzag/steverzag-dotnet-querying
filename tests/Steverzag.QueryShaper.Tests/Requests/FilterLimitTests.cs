using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Configuration;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary><see cref="QueryShaperOptions.MaxFilters"/>: every node of the filter tree counts, groups included, so the cap bounds the depth too.</summary>
	public class FilterLimitTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

		private static ServiceProvider Provider(int? max = null)
			=> new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>(o => { if (max is { } m) o.MaxFilters = m; })
				.BuildServiceProvider();

		private static IQueryShaper Shaper(int? max = null) => Provider(max).GetRequiredService<IQueryShaper>();

		private static ConditionNode Positive(string field = nameof(Product.Quantity))
			=> new() { Field = field, Condition = FilterCondition.GreaterThan, Value = 0 };

		private static FilterNode[] Flat(int count) => [.. Enumerable.Range(0, count).Select(_ => Positive())];

		private static FilterNode Nested(int groups)
		{
			FilterNode node = Positive();
			for (var i = 0; i < groups; i++)
			{
				node = new GroupNode { Filters = [node] };
			}
			return node;
		}

		private int[] Ids(Func<IQueryable<Product>, IQueryable<Product>> apply)
		{
			using var context = Database.CreateContext();
			return [.. apply(context.Products).Select(p => p.Id)];
		}

		[Fact]
		public void TheDefault_Is50()
			=> Assert.Equal(50, new QueryShaperOptions().MaxFilters);

		[Fact]
		public void ExactlyAtTheLimit_Runs()
		{
			var shaper = Shaper(3);
			Assert.Equal([1, 3, 4], Ids(q => shaper.ApplyFilters(q, Flat(3)).OrderBy(p => p.Id)));
		}

		[Fact]
		public void OneOverTheLimit_IsARequestError()
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(3).ApplyFilters(context.Products, Flat(4)));

			Assert.Equal("", ex.Path);
			Assert.Equal("A request can have at most 3 filters, groups included.", ex.Detail);
		}

		[Fact]
		public void TheDefaultLimit_Applies()
		{
			using var context = Database.CreateContext();

			_ = Shaper().ApplyFilters(context.Products, Flat(50));
			Assert.Throws<QueryRequestException>(() => Shaper().ApplyFilters(context.Products, Flat(51)));
		}

		[Fact]
		public void AGroup_CountsAsAFilter()
		{
			using var context = Database.CreateContext();
			FilterNode[] atLimit = [Positive(), new GroupNode { Filters = [Positive()] }];
			FilterNode[] overLimit = [Positive(), new GroupNode { Filters = [Positive(), Positive()] }];

			_ = Shaper(3).ApplyFilters(context.Products, atLimit);
			Assert.Throws<QueryRequestException>(() => Shaper(3).ApplyFilters(context.Products, overLimit));
		}

		[Fact]
		public void NestedGroups_CountEachLevel()
		{
			using var context = Database.CreateContext();

			_ = Shaper(5).ApplyFilters(context.Products, [Nested(4)]);
			Assert.Throws<QueryRequestException>(() => Shaper(5).ApplyFilters(context.Products, [Nested(5)]));
		}

		[Fact]
		public void AVeryDeepTree_IsRejectedWithoutCrashing()
		{
			using var context = Database.CreateContext();

			var ex = Assert.Throws<QueryRequestException>(() => Shaper().ApplyFilters(context.Products, [Nested(100_000)]));

			Assert.Equal("A request can have at most 50 filters, groups included.", ex.Detail);
		}

		[Fact]
		public void AVeryWideTree_IsRejected()
		{
			using var context = Database.CreateContext();
			Assert.Throws<QueryRequestException>(() => Shaper().ApplyFilters(context.Products, Flat(100_000)));
		}

		[Fact]
		public void TheLimit_IsCheckedBeforeTheSelectors()
		{
			using var context = Database.CreateContext();
			FilterNode[] filters = [Positive("Nope"), Positive(), Positive()];

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(2).ApplyFilters(context.Products, filters));

			Assert.Equal("A request can have at most 2 filters, groups included.", ex.Detail);
		}

		[Fact]
		public void ABoundRequest_ReportsTheLimitAtFilters()
		{
			using var context = Database.CreateContext();
			var request = JsonSerializer.Deserialize<QueryRequest>("""
				{ "filters": [ { "field": "Quantity", "condition": "GreaterThan", "value": 0 },
				               { "connector": "Or", "filters": [ { "field": "Name", "condition": "IsEmpty" },
				                                                  { "field": "Name", "condition": "IsNotEmpty" } ] } ] }
				""", Web)!;

			var ex = Assert.Throws<QueryRequestException>(() => Shaper(3).Apply(context.Products, request));

			Assert.Equal("filters", ex.Path);
			Assert.Equal("filters: A request can have at most 3 filters, groups included.", ex.Message);
		}

		[Fact]
		public void TheTrustedExtensions_DontCheck()
		{
			Assert.Equal([1, 3, 4], Ids(q => q.ApplyLogicsFilter(Flat(60)).OrderBy(p => p.Id)));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		public void AMaximumBelowOne_FailsWhenTheShaperIsResolved(int max)
		{
			using var provider = Provider(max);

			var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryShaper>());
			Assert.Equal("QueryShaperOptions: MaxFilters must be at least 1.", ex.Message);
		}
	}
}
