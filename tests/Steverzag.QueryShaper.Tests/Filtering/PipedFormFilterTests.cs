using Microsoft.EntityFrameworkCore;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class PipedFormFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// 1 Hammer | Price 19.99 | Quantity 100 | Rating 4 | Score 4.5  | CreatedAt 2024-01-10 | ReleaseDate 2024-01-01 | Description "Steel head"  | max stars 5
		// 2 Shovel | Price 0     | Quantity 0   | Rating 0 | Score 0    | CreatedAt 2024-03-15 | ReleaseDate 2024-03-01 | Description null          | max stars 3
		// 3 Wrench | Price 9.99  | Quantity 50  | Rating 2 | Score 2.25 | CreatedAt 2024-06-20 | ReleaseDate 2024-06-01 | Description ""            | no reviews
		// 4 Rake   | Price 29.99 | Quantity 200 | Rating 5 | Score 5    | CreatedAt 2024-09-05 | ReleaseDate 2024-09-01 | Description "Garden rake" | max stars 4

		public static TheoryData<string, string, FilterCondition, object, int[]> Pairs => new()
		{
			{ "Price|$FN_MATH_ROUND(1)", "$FN_MATH_ROUND(Price, 1)", FilterCondition.Equals, 20, [1] },
			{ "Rating|$FN_MATH_MAX(Quantity)", "$FN_MATH_MAX(Rating, Quantity)", FilterCondition.GreaterThan, 60, [1, 4] },
			{ "Score|$FN_MATH_POW(2)", "$FN_MATH_POW(Score, 2)", FilterCondition.GreaterThan, 20, [1, 4] },
			{ "CreatedAt|$FN_DT_ADDDAYS(3)", "$FN_DT_ADDDAYS(CreatedAt, 3)", FilterCondition.GreaterThan, new DateTime(2024, 6, 22), [3, 4] },
			// Day counts come from the root's Quantity: 2024-04-19, 2024-03-15, 2024-08-09, 2025-03-24.
			{ "CreatedAt|$FN_DT_ADDDAYS(Quantity)", "$FN_DT_ADDDAYS(CreatedAt, Quantity)", FilterCondition.LessThan, new DateTime(2024, 5, 1), [1, 2] },
			{ "ReleaseDate|$FN_DT_ADDMONTHS(1)", "$FN_DT_ADDMONTHS(ReleaseDate, 1)", FilterCondition.Equals, new DateOnly(2024, 2, 1), [1] },
			{ "Name|$FN_STR_CONCAT(\"!\")", "$FN_STR_CONCAT(Name, \"!\")", FilterCondition.Equals, "Hammer!", [1] },
			{ "Description|$FN_STR_ISNULLOREMPTY", "$FN_STR_ISNULLOREMPTY(Description)", FilterCondition.Equals, true, [2, 3] },
			{
				"Reviews.Stars|$FN_AGGR_MAX|$FN_COALESCE(0)|$FN_MATH_MAX(3)",
				"$FN_MATH_MAX(Reviews.Stars|$FN_AGGR_MAX|$FN_COALESCE(0), 3)",
				FilterCondition.Equals, 3, [2, 3]
			},
		};

		[Theory]
		[MemberData(nameof(Pairs))]
		public void PipedForm_FiltersLikeTheStaticForm(string piped, string @static, FilterCondition condition, object value, int[] expected)
		{
			Assert.Equal(expected, FilterIds(piped, condition, ToValue(value)));
			Assert.Equal(expected, FilterIds(@static, condition, ToValue(value)));
		}

		[Theory]
		[MemberData(nameof(Pairs))]
		public void PipedForm_TranslatesToTheSameSqlAsTheStaticForm(string piped, string @static, FilterCondition condition, object value, int[] _)
		{
			Assert.Equal(Sql(@static, condition, value), Sql(piped, condition, value));
		}

		[Fact]
		public void OrderingByAPipedSelector()
		{
			using var context = Database.CreateContext();
			var ids = context.Products
				.ApplyOrder((PropertySelector)"CreatedAt|$FN_DT_ADDDAYS(Quantity)", OrderDirection.Ascending)
				.Select(p => p.Id)
				.ToArray();

			Assert.Equal([2, 1, 3, 4], ids);
		}

		private string Sql(string field, FilterCondition condition, object value)
		{
			using var context = Database.CreateContext();
			return context.Products.ApplyLogicsFilter([Node(field, condition, ToValue(value))]).ToQueryString();
		}
	}
}
