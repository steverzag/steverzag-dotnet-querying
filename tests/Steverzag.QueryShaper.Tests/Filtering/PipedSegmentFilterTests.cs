using Microsoft.EntityFrameworkCore;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class PipedSegmentFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		[Fact]
		public void AMemberAfterADateFunction_Filters()
		{
			Assert.Equal([2], FilterIds("CreatedAt|$FN_DT_ADDDAYS(30)|Month", FilterCondition.Equals, 4));
		}

		[Fact]
		public void AMemberAfterAnElementFunction_Filters()
		{
			Assert.Equal([4], FilterIds("Reviews|$FN_ORDER_DESC(Stars)|$FN_ELM_FIRST|Stars", FilterCondition.Equals, 4));
		}

		[Fact]
		public void APipedNavigation_FiltersLikeTheDottedPath()
		{
			Assert.Equal(FilterIds("Category.Name", FilterCondition.Equals, "Garden"), FilterIds("Category|Name", FilterCondition.Equals, "Garden"));
		}

		[Fact]
		public void ACollectionPipedIntoAProperty_Projects()
		{
			Assert.Equal([1, 4], FilterIds("Reviews|Stars|$FN_AGGR_SUM", FilterCondition.GreaterThan, 5));
		}

		[Fact]
		public void AMemberAfterADateFunction_Translates()
		{
			using var context = Database.CreateContext();
			var sql = context.Products.ApplyLogicsFilter([Node("CreatedAt|$FN_DT_ADDDAYS(30)|Month", FilterCondition.Equals, 4)]).ToQueryString();

			Assert.Contains("strftime('%m', \"p\".\"CreatedAt\", CAST(30.0 AS TEXT) || ' days')", sql);
		}
	}
}
