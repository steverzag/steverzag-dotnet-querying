using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class NumericFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		/// <summary>
		/// Every non-nullable numeric property has the same shape in the seed: row 1 high, row 2 zero,
		/// row 3 the mid value, row 4 highest. So, compared with the mid value, every type expects the same ids.
		/// </summary>
		private static readonly (FilterCondition Condition, int[] Expected)[] ComparisonsAgainstMid =
		[
			(FilterCondition.GreaterThan, [1, 4]),
			(FilterCondition.GreaterThanOrEqual, [1, 3, 4]),
			(FilterCondition.LessThan, [2]),
			(FilterCondition.LessThanOrEqual, [2, 3]),
			(FilterCondition.Equals, [3]),
			(FilterCondition.NotEquals, [1, 2, 4]),
		];

		private static readonly (string Field, object Mid)[] NumericFields =
		[
			(nameof(Product.Rating), SeedData.Mid.Rating),
			(nameof(Product.Stock), SeedData.Mid.Stock),
			(nameof(Product.Quantity), SeedData.Mid.Quantity),
			(nameof(Product.Views), SeedData.Mid.Views),
			(nameof(Product.Weight), SeedData.Mid.Weight),
			(nameof(Product.Score), SeedData.Mid.Score),
			(nameof(Product.Price), SeedData.Mid.Price),
		];

		public static TheoryData<string, object, FilterCondition, int[]> ComparisonCases()
		{
			var data = new TheoryData<string, object, FilterCondition, int[]>();
			foreach (var (field, mid) in NumericFields)
				foreach (var (condition, expected) in ComparisonsAgainstMid)
					data.Add(field, mid, condition, expected);
			return data;
		}

		public static TheoryData<string> Fields()
		{
			var data = new TheoryData<string>();
			foreach (var (field, _) in NumericFields)
				data.Add(field);
			return data;
		}

		[Theory]
		[MemberData(nameof(ComparisonCases))]
		public void Comparison_AgainstMidValue(string field, object mid, FilterCondition condition, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, condition, ToValue(mid)));
		}

		[Theory]
		[MemberData(nameof(Fields))]
		public void IsEmpty_OnNonNullable_MatchesZero(string field)
		{
			Assert.Equal([2], FilterIds(field, FilterCondition.IsEmpty));
		}

		[Theory]
		[MemberData(nameof(Fields))]
		public void IsNotEmpty_OnNonNullable_ExcludesZero(string field)
		{
			Assert.Equal([1, 3, 4], FilterIds(field, FilterCondition.IsNotEmpty));
		}

		[Theory]
		[MemberData(nameof(Fields))]
		public void NullValue_IsTreatedAsZero(string field)
		{
			Assert.Equal([2], FilterIds(field, FilterCondition.Equals));
		}

		// NullableQuantity: 1 → 100, 2 → null, 3 → 0, 4 → 200

		[Fact]
		public void Nullable_GreaterThan_ExcludesNullRows()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.NullableQuantity), FilterCondition.GreaterThan, 50));
		}

		[Fact]
		public void Nullable_Equals()
		{
			Assert.Equal([3], FilterIds(nameof(Product.NullableQuantity), FilterCondition.Equals, 0));
		}

		[Fact]
		public void Nullable_NotEquals_IncludesNullRows()
		{
			Assert.Equal([2, 3, 4], FilterIds(nameof(Product.NullableQuantity), FilterCondition.NotEquals, 100));
		}

		[Fact]
		public void Nullable_IsEmpty_MatchesNullAndZero()
		{
			// Current semantics: "empty" for a nullable number means null OR zero, via coalesce(x, 0) = 0.
			// This is what the code does today. It may not be what we want (see ROADMAP Phase 2).
			Assert.Equal([2, 3], FilterIds(nameof(Product.NullableQuantity), FilterCondition.IsEmpty));
		}

		[Fact]
		public void Nullable_IsNotEmpty_ExcludesNullAndZero()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.NullableQuantity), FilterCondition.IsNotEmpty));
		}

		[Theory]
		[InlineData(FilterCondition.StartsWith)]
		[InlineData(FilterCondition.Contains)]
		[InlineData(FilterCondition.ContainsAny)]
		public void UnsupportedCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.Quantity), condition, 1));
		}

		[Fact]
		public void MismatchedValueKind_Throws()
		{
			// The value is cast to the property type; a string for an int property is rejected.
			Assert.Throws<InvalidCastException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.Equals, "50"));
		}
	}
}
