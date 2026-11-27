using Microsoft.Extensions.DependencyInjection;
using Steverzag.ProviderTests.Infrastructure;
using Steverzag.QueryShaper;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Pagination;

namespace Steverzag.ProviderTests
{
	/// <summary>
	/// <c>ApplyPagedAsync</c> falls back to the primary key (decision 051), as the whole order or as a tie-breaker, and a
	/// request that already orders by the key runs on every provider.
	/// </summary>
	public abstract class DefaultOrderProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
		where TFixture : ProviderFixture
	{
		// Text: 1 "b", 2 "ab", 3 "a"
		private static readonly IQueryShaper Shaper = new ServiceCollection()
			.AddQueryShaper<NoConfiguration>()
			.BuildServiceProvider()
			.GetRequiredService<IQueryShaper>();

		private async Task<int[]> Ids(params OrderParameters[] order)
		{
			await using var context = fixture.CreateContext();
			var page = await Shaper.ApplyPagedAsync(context.Scalars, new QueryRequest { Order = order });
			return [.. page.Select(s => s.Id)];
		}

		[Fact]
		public async Task NoOrder_PagesByThePrimaryKey()
		{
			var ids = await Ids();
			Assert.Equal([1, 2, 3], ids);
		}

		[Fact]
		public async Task ARequestOrder_GetsThePrimaryKeyAsATieBreaker()
		{
			// Flag: 2 false; 1 and 3 true, tied
			var ids = await Ids(new OrderParameters(nameof(Scalar.Flag)));
			Assert.Equal([2, 1, 3], ids);
		}

		[Fact]
		public async Task ARequestOrderOnTheKey_IsntRepeated()
		{
			var ids = await Ids(new OrderParameters(nameof(Scalar.Id), OrderDirection.Descending));
			Assert.Equal([3, 2, 1], ids);
		}
	}

	public sealed class SqliteDefaultOrderTests(SqliteFixture fixture) : DefaultOrderProviderTests<SqliteFixture>(fixture);

	public sealed class SqlServerDefaultOrderTests(SqlServerFixture fixture) : DefaultOrderProviderTests<SqlServerFixture>(fixture);

	public sealed class PostgreSqlDefaultOrderTests(PostgreSqlFixture fixture) : DefaultOrderProviderTests<PostgreSqlFixture>(fixture);
}
