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

		[Theory]
		[InlineData(nameof(Product.CreatedAt))]
		[InlineData(nameof(Product.DeletedAt))]
		[InlineData(nameof(Product.ReleaseDate))]
		[InlineData(nameof(Product.DiscontinuedOn))]
		[InlineData(nameof(Product.OpensAt))]
		[InlineData(nameof(Product.LastServicedAt))]
		public void NullValue_Throws(string field)
		{
			// Decision 013.2. This used to be a bare InvalidOperationException from Nullable<T>.Value.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.GreaterThan));
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.Equals));
		}

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

		// Nullable dates (DateTime?, DateOnly?, TimeOnly?): only row 3 has a value.

		private static readonly (string Field, object Row3)[] NullableDateFields =
		[
			(nameof(Product.DeletedAt), SeedData.Row3NullableDate.DeletedAt),
			(nameof(Product.DiscontinuedOn), SeedData.Row3NullableDate.DiscontinuedOn),
			(nameof(Product.LastServicedAt), SeedData.Row3NullableDate.LastServicedAt),
		];

		/// <summary>Compared with row 3's own value, null rows never match, except for NotEquals (C# semantics).</summary>
		private static readonly (FilterCondition Condition, int[] Expected)[] NullableComparisonsAgainstRow3 =
		[
			(FilterCondition.GreaterThan, []),
			(FilterCondition.GreaterThanOrEqual, [3]),
			(FilterCondition.LessThan, []),
			(FilterCondition.LessThanOrEqual, [3]),
			(FilterCondition.Equals, [3]),
			(FilterCondition.NotEquals, [1, 2, 4]),
		];

		public static TheoryData<string, object, FilterCondition, int[]> NullableComparisonCases()
		{
			var data = new TheoryData<string, object, FilterCondition, int[]>();
			foreach (var (field, row3) in NullableDateFields)
				foreach (var (condition, expected) in NullableComparisonsAgainstRow3)
					data.Add(field, row3, condition, expected);
			return data;
		}

		public static TheoryData<string> NullableFields()
		{
			var data = new TheoryData<string>();
			foreach (var (field, _) in NullableDateFields)
				data.Add(field);
			return data;
		}

		[Theory]
		[MemberData(nameof(NullableComparisonCases))]
		public void Nullable_Comparison_AgainstRow3Value(string field, object row3, FilterCondition condition, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, condition, ToValue(row3)));
		}

		[Theory]
		[MemberData(nameof(NullableFields))]
		public void Nullable_IsEmpty_MatchesNull(string field)
		{
			Assert.Equal([1, 2, 4], FilterIds(field, FilterCondition.IsEmpty));
		}

		[Theory]
		[MemberData(nameof(NullableFields))]
		public void Nullable_IsNotEmpty_ExcludesNull(string field)
		{
			Assert.Equal([3], FilterIds(field, FilterCondition.IsNotEmpty));
		}
	}
}
