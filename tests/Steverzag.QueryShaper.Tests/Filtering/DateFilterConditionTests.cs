using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class DateFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		/// <summary>Date properties ascend by id, and row 2 holds the mid value.</summary>
		private static readonly (FilterCondition Condition, int[] Expected)[] ComparisonsAgainstMid =
		[
			(FilterCondition.GreaterThan, [3, 4]),
			(FilterCondition.GreaterThanOrEqual, [2, 3, 4]),
			(FilterCondition.LessThan, [1]),
			(FilterCondition.LessThanOrEqual, [1, 2]),
			(FilterCondition.Equals, [2]),
			(FilterCondition.NotEquals, [1, 3, 4]),
		];

		private static readonly (string Field, object Mid)[] DateFields =
		[
			(nameof(Product.CreatedAt), SeedData.MidDate.CreatedAt),
			(nameof(Product.ReleaseDate), SeedData.MidDate.ReleaseDate),
			(nameof(Product.OpensAt), SeedData.MidDate.OpensAt),
		];

		public static TheoryData<string, object, FilterCondition, int[]> ComparisonCases()
		{
			var data = new TheoryData<string, object, FilterCondition, int[]>();
			foreach (var (field, mid) in DateFields)
				foreach (var (condition, expected) in ComparisonsAgainstMid)
					data.Add(field, mid, condition, expected);
			return data;
		}

		public static TheoryData<string, FilterCondition> NonNullableEmptinessCases()
		{
			var data = new TheoryData<string, FilterCondition>();
			foreach (var (field, _) in DateFields)
			{
				data.Add(field, FilterCondition.IsEmpty);
				data.Add(field, FilterCondition.IsNotEmpty);
			}
			return data;
		}

		[Theory]
		[MemberData(nameof(ComparisonCases))]
		public void Comparison_AgainstMidValue(string field, object mid, FilterCondition condition, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, condition, ToValue(mid)));
		}

		[Theory]
		[MemberData(nameof(NonNullableEmptinessCases))]
		public void NonNullable_EmptinessCondition_Throws(string field, FilterCondition condition)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, condition));
		}

		[Fact]
		public void UnsupportedCondition_Throws()
		{
			Assert.Throws<FilterNotSupportedException>(
				() => FilterIds(nameof(Product.CreatedAt), FilterCondition.StartsWith, SeedData.MidDate.CreatedAt));
		}

		// DeletedAt (DateTime?): only row 3 has a value (2024-07-01)

		[Fact]
		public void NullableDateTime_IsEmpty_MatchesNull()
		{
			Assert.Equal([1, 2, 4], FilterIds(nameof(Product.DeletedAt), FilterCondition.IsEmpty));
		}

		[Fact]
		public void NullableDateTime_IsNotEmpty_ExcludesNull()
		{
			Assert.Equal([3], FilterIds(nameof(Product.DeletedAt), FilterCondition.IsNotEmpty));
		}

		private const string NullableDateBug =
			"Known bug: the constant is typed DateTime, not DateTime?, so the comparison cannot be built. DateOnly?/TimeOnly? share the code.";

		[Fact(Skip = NullableDateBug)]
		public void NullableDateTime_GreaterThan()
		{
			Assert.Equal([3], FilterIds(nameof(Product.DeletedAt), FilterCondition.GreaterThan, new DateTime(2024, 1, 1)));
		}

		[Fact(Skip = NullableDateBug)]
		public void NullableDateTime_Equals()
		{
			Assert.Equal([3], FilterIds(nameof(Product.DeletedAt), FilterCondition.Equals, new DateTime(2024, 7, 1)));
		}
	}
}
