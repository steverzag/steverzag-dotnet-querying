using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class DatePartFilterTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		[Theory]
		[InlineData("CreatedAt.Year", 2024, new[] { 1, 2, 3, 4 })]
		[InlineData("CreatedAt.Month", 3, new[] { 2 })]
		[InlineData("CreatedAt.Day", 20, new[] { 3 })]
		[InlineData("CreatedAt.Hour", 16, new[] { 4 })]
		[InlineData("CreatedAt.Minute", 30, new[] { 4 })]
		[InlineData("CreatedAt.DayOfYear", 75, new[] { 2 })]
		[InlineData("ReleaseDate.Year", 2024, new[] { 1, 2, 3, 4 })]
		[InlineData("ReleaseDate.Month", 6, new[] { 3 })]
		[InlineData("DeletedAt.Value.Month", 7, new[] { 3 })]
		public void DatePart_Equals(string field, int value, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, FilterCondition.Equals, value));
		}

		[Theory]
		[InlineData("CreatedAt.Month", 3, new[] { 3, 4 })]
		[InlineData("ReleaseDate.Month", 3, new[] { 3, 4 })]
		public void DatePart_GreaterThan(string field, int value, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, FilterCondition.GreaterThan, value));
		}

		[Theory]
		[InlineData("CreatedAt.DayOfWeek", nameof(DayOfWeek.Thursday), new[] { 3, 4 })]
		[InlineData("ReleaseDate.DayOfWeek", nameof(DayOfWeek.Friday), new[] { 2 })]
		public void DatePart_DayOfWeek_Equals(string field, string day, int[] expected)
		{
			Assert.Equal(expected, FilterIds(field, FilterCondition.Equals, day));
		}

		[Theory]
		[InlineData("OpensAt.Hour")]
		[InlineData("OpensAt.Minute")]
		public void TimeOnlyPart_DoesNotTranslateOnSqlite(string field)
		{
			Assert.Throws<InvalidOperationException>(() => FilterIds(field, FilterCondition.Equals, 9));
		}

		[Theory]
		[InlineData("DeletedAt.Year", 2024)]
		[InlineData("DeletedAt.Month", 7)]
		[InlineData("DiscontinuedOn.Month", 7)]
		[InlineData("DeletedAt.Date.Day", 1)]
		public void NullableDatePart_Equals_MatchesOnlyTheRowWithAValue(string field, int value)
		{
			Assert.Equal([3], FilterIds(field, FilterCondition.Equals, value));
		}

		[Theory]
		[InlineData("DeletedAt.Year")]
		[InlineData("DiscontinuedOn.Month")]
		public void NullableDatePart_IsEmpty_MatchesTheNullRows(string field)
		{
			Assert.Equal([1, 2, 4], FilterIds(field, FilterCondition.IsEmpty));
			Assert.Equal([3], FilterIds(field, FilterCondition.IsNotEmpty));
		}

		[Fact]
		public void NullableDatePart_NotEquals_KeepsTheNullRows()
		{
			Assert.Equal([1, 2, 4], FilterIds("DeletedAt.Month", FilterCondition.NotEquals, 7));
		}

		[Fact]
		public void DatePart_Date_TruncatesTheTime()
		{
			Assert.Equal([2], FilterIds("CreatedAt.Date", FilterCondition.Equals, new DateTime(2024, 3, 15)));
		}
	}
}
