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
		public void NullValue_Throws(string field)
		{
			// Decision 013.2. A missing value used to be read as 0, so "Quantity = null" matched "Quantity = 0".
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.Equals));
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.GreaterThan));
		}

		[Fact]
		public void TypedNullValue_Throws()
		{
			// A FilterValue that holds a null (from an int? variable) is a missing value too.
			int? missing = null;
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.Equals, missing));
		}

		[Fact]
		public void NullValue_OnANullableProperty_Throws_UseIsEmptyInstead()
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.NullableQuantity), FilterCondition.Equals));
		}

		// ---- a single value converts like a list value (decision 013.1)

		[Theory]
		[InlineData(nameof(Product.Score), 1L)]     // double ← integer
		[InlineData(nameof(Product.Price), 1L)]     // decimal ← integer
		[InlineData(nameof(Product.Weight), 0L)]    // float ← integer
		[InlineData(nameof(Product.Quantity), 1.0)] // int ← whole double
		[InlineData(nameof(Product.Views), 1.0)]    // long ← whole double
		public void AnyNumberThatFits_Converts(string field, object value)
		{
			// Rows 1, 3 and 4 are above 1 (row 2 is 0) for every numeric property.
			Assert.Equal([1, 3, 4], FilterIds(field, FilterCondition.GreaterThan, ToValue(value)));
		}

		[Fact]
		public void Equals_WithAnotherNumberKind_MatchesExactly()
		{
			Assert.Equal([3], FilterIds(nameof(Product.Quantity), FilterCondition.Equals, 50.0));
			Assert.Equal([3], FilterIds(nameof(Product.Score), FilterCondition.Equals, 2.25m));
			Assert.Equal([3], FilterIds(nameof(Product.Price), FilterCondition.Equals, 9.99));
		}

		[Theory]
		[InlineData(50.5)]
		[InlineData(double.NaN)]
		[InlineData(double.PositiveInfinity)]
		[InlineData(1e20)]
		public void IntegerProperty_WithAFractionalOrNonFiniteOrHugeValue_Throws(double value)
		{
			// Rejected, not truncated: 50.5 used to be an InvalidCastException and is never read as 50.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.Equals, value));
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

		// A single integer value is stored as long and narrowed to the property type with checked casts:
		// values that fit work, values that don't throw instead of wrapping around.
		// Quantity/Stock: 1 → 100, 2 → 0, 3 → 50, 4 → 200. Rating: 4, 0, 2, 5.

		[Theory]
		[InlineData(nameof(Product.Quantity), 50L, new[] { 3 })]
		[InlineData(nameof(Product.Stock), 50L, new[] { 3 })]
		[InlineData(nameof(Product.Rating), 4L, new[] { 1 })]
		[InlineData(nameof(Product.NullableQuantity), 100L, new[] { 1 })]
		public void Equals_WithLongValueThatFits(string field, long value, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, FilterCondition.Equals, value));
		}

		[Theory]
		[InlineData(nameof(Product.Quantity), 4_294_967_346L)] // 2^32 + 50: used to wrap to 50 and match row 3
		[InlineData(nameof(Product.Quantity), (long)int.MaxValue + 1)]
		[InlineData(nameof(Product.Quantity), (long)int.MinValue - 1)]
		[InlineData(nameof(Product.NullableQuantity), 4_294_967_396L)] // 2^32 + 100: used to wrap to 100
		[InlineData(nameof(Product.Stock), 65_586L)] // 2^16 + 50: used to wrap to 50
		[InlineData(nameof(Product.Stock), -40_000L)]
		[InlineData(nameof(Product.Rating), 260L)] // 256 + 4: used to wrap to 4
		[InlineData(nameof(Product.Rating), -1L)]
		public void Equals_WithIntegerThatDoesNotFit_Throws(string field, long value)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.Equals, value));
		}

		[Theory]
		[InlineData(FilterCondition.GreaterThan)]
		[InlineData(FilterCondition.LessThanOrEqual)]
		[InlineData(FilterCondition.NotEquals)]
		public void Comparison_WithIntegerThatDoesNotFit_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Quantity), condition, 4_294_967_346L));
		}

		[Fact]
		public void Float_WithDoubleBeyondFloatRange_Throws()
		{
			// double → float gives infinity instead of throwing, so it's checked separately.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Weight), FilterCondition.LessThan, 1e40));
		}

		[Fact]
		public void Decimal_WithDoubleBeyondDecimalRange_Throws()
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Price), FilterCondition.LessThan, 1e30));
		}

		[Fact]
		public void Long_WithLargestLong_Works()
		{
			// Views: 1000, 0, 500, 2000. No narrowing, so nothing to check.
			Assert.Equal([1, 2, 3, 4], FilterIds(nameof(Product.Views), FilterCondition.LessThan, long.MaxValue));
		}

		[Fact]
		public void MismatchedValueKind_Throws()
		{
			// Only numbers convert: a numeric string or a bool for a number property is rejected.
			Assert.Throws<InvalidCastException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.Equals, "50"));
			Assert.Throws<InvalidCastException>(() => FilterIds(nameof(Product.Score), FilterCondition.Equals, true));
		}
	}
}
