using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Requests
{
	/// <summary>
	/// A string value is converted to the property's type when the filter is built, strictly and in ISO 8601,
	/// because JSON has no date type (decision 041.4).
	/// </summary>
	public class TextValueConversionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		[Theory]
		[InlineData("2024-03-15", FilterCondition.GreaterThanOrEqual, new[] { 2, 3, 4 })]
		[InlineData("2024-03-15T10:00", FilterCondition.Equals, new[] { 2 })]
		[InlineData("2024-03-15T10:00:00", FilterCondition.Equals, new[] { 2 })]
		[InlineData("2024-03-15T10:00:00.0000000", FilterCondition.Equals, new[] { 2 })]
		[InlineData("2024-03-15T10:00:00.5", FilterCondition.LessThan, new[] { 1, 2 })]
		[InlineData("2024-03-15T10:00:00Z", FilterCondition.Equals, new[] { 2 })]
		[InlineData("2024-03-15T12:00:00+02:00", FilterCondition.Equals, new[] { 2 })]
		[InlineData("2024-03-15T05:00:00-05:00", FilterCondition.Equals, new[] { 2 })]
		public void ADateTime_IsReadFromIsoText(string text, FilterCondition condition, int[] expected)
		{
			Assert.Equal(expected, FilterIds(nameof(Product.CreatedAt), condition, text));
		}

		[Fact]
		public void ADateTimeWithAnOffset_IsConvertedToUtc_AndOneWithoutIsTakenAsWritten()
		{
			Product utc = new() { Id = 1, CreatedAt = new DateTime(2024, 3, 15, 10, 0, 0, DateTimeKind.Utc) };
			Product local = new() { Id = 2, CreatedAt = new DateTime(2024, 3, 15, 12, 0, 0) };

			var matches = new[] { utc, local }.AsQueryable()
				.ApplyLogicsFilter([Node(nameof(Product.CreatedAt), FilterCondition.Equals, "2024-03-15T12:00:00+02:00")])
				.ToArray();

			Assert.Same(utc, Assert.Single(matches));
		}

		[Fact]
		public void ANullableDateTime_IsReadFromText()
		{
			Assert.Equal([3], FilterIds(nameof(Product.DeletedAt), FilterCondition.Equals, "2024-07-01"));
		}

		[Theory]
		[InlineData("2024-03-01", FilterCondition.Equals, new[] { 2 })]
		[InlineData("2024-06-01", FilterCondition.LessThan, new[] { 1, 2 })]
		public void ADateOnly_IsReadFromIsoText(string text, FilterCondition condition, int[] expected)
		{
			Assert.Equal(expected, FilterIds(nameof(Product.ReleaseDate), condition, text));
		}

		[Theory]
		[InlineData("09:30", FilterCondition.Equals, new[] { 2 })]
		[InlineData("09:30:00", FilterCondition.Equals, new[] { 2 })]
		[InlineData("09:30:00.0000000", FilterCondition.Equals, new[] { 2 })]
		[InlineData("12:00", FilterCondition.GreaterThanOrEqual, new[] { 3, 4 })]
		public void ATimeOnly_IsReadFromIsoText(string text, FilterCondition condition, int[] expected)
		{
			Assert.Equal(expected, FilterIds(nameof(Product.OpensAt), condition, text));
		}

		[Theory]
		[InlineData(nameof(Product.CreatedAt), "15/03/2024", "DateTime")]
		[InlineData(nameof(Product.CreatedAt), "2024-3-15", "DateTime")]
		[InlineData(nameof(Product.CreatedAt), "2024-03-15 10:00:00", "DateTime")]
		[InlineData(nameof(Product.CreatedAt), "2024-03-15t10:00:00", "DateTime")]
		[InlineData(nameof(Product.CreatedAt), "March 15, 2024", "DateTime")]
		[InlineData(nameof(Product.CreatedAt), "2024-02-30", "DateTime")]
		[InlineData(nameof(Product.CreatedAt), "", "DateTime")]
		[InlineData(nameof(Product.ReleaseDate), "2024-03-01T00:00:00", "DateOnly")]
		[InlineData(nameof(Product.ReleaseDate), "01/03/2024", "DateOnly")]
		[InlineData(nameof(Product.OpensAt), "9:30", "TimeOnly")]
		[InlineData(nameof(Product.OpensAt), "09:30 PM", "TimeOnly")]
		[InlineData(nameof(Product.OpensAt), "24:00", "TimeOnly")]
		public void TextThatIsntIso_IsRejected_NamingTheField(string field, string text, string type)
		{
			var ex = Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.Equals, text));

			Assert.StartsWith($"The filter value for '{field}' is not a valid {type}: '{text}'. Use ", ex.Message);
		}

		[Fact]
		public void AValueTypedInCode_IsUnchanged()
		{
			Assert.Equal([2], FilterIds(nameof(Product.CreatedAt), FilterCondition.Equals, new DateTime(2024, 3, 15, 10, 0, 0)));
		}

		[Fact]
		public void ANonTextValueOfTheWrongKind_StillFailsAsBefore()
		{
			Assert.Throws<InvalidCastException>(() => FilterIds(nameof(Product.CreatedAt), FilterCondition.Equals, 20240315));
		}

		[Fact]
		public void DateTimeTextInAList_IsConverted()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.CreatedAt), FilterCondition.In, new List<object> { "2024-01-10T08:00:00", "2024-09-05T16:30:00" }));
		}

		[Fact]
		public void DateOnlyTextInAList_IsConverted()
		{
			Assert.Equal([2, 3], FilterIds(nameof(Product.ReleaseDate), FilterCondition.In, new List<object> { "2024-03-01", "2024-06-01" }));
		}

		[Fact]
		public void TimeSpanTextInAList_IsConverted()
		{
			Assert.Equal([1, 3], FilterIds(nameof(Product.Warranty), FilterCondition.In, new List<object> { "365.00:00:00", "00:00:00" }));
		}

		[Fact]
		public void CharTextInAList_IsConverted()
		{
			Assert.Equal([1, 2, 4], FilterIds(nameof(Product.Grade), FilterCondition.NotIn, new List<object> { "C" }));
		}

		[Fact]
		public void GuidTextInAList_IsConverted()
		{
			Assert.Equal([1, 3], FilterIds(nameof(Product.Code), FilterCondition.In, new List<object> { SeedData.Code1.ToString(), SeedData.Code3.ToString("B") }));
		}

		[Fact]
		public void ANullableListElement_StillAcceptsNull()
		{
			Assert.Equal([1, 2, 4], FilterIds(nameof(Product.DeletedAt), FilterCondition.In, new List<object> { null!, "2020-01-01" }));
		}

		[Theory]
		[InlineData(nameof(Product.CreatedAt), "yesterday", "DateTime")]
		[InlineData(nameof(Product.Warranty), "1 day", "TimeSpan")]
		[InlineData(nameof(Product.Grade), "AB", "Char")]
		[InlineData(nameof(Product.Grade), "", "Char")]
		[InlineData(nameof(Product.Code), "not-a-guid", "Guid")]
		public void InvalidTextInAList_IsRejected_NamingTheField(string field, string text, string type)
		{
			var ex = Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.In, new List<object> { text }));

			Assert.StartsWith($"The filter value for '{field}' is not a valid {type}: '{text}'.", ex.Message);
		}

		private sealed class Event(DateTimeOffset at)
		{
			public DateTimeOffset At { get; } = at;
		}

		private static readonly Event[] Events =
		[
			new(new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero)),
			new(new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.FromHours(2))),
			new(new DateTimeOffset(2024, 3, 16, 10, 0, 0, TimeSpan.Zero)),
		];

		private static int[] EventIndexes(List<object> values)
		{
			var matches = Events.AsQueryable().ApplyLogicsFilter([Node(nameof(Event.At), FilterCondition.In, values)]).ToArray();
			return [.. matches.Select(e => Array.IndexOf(Events, e))];
		}

		[Theory]
		[InlineData("2024-03-15T10:00:00Z")]
		[InlineData("2024-03-15T10:00:00+00:00")]
		[InlineData("2024-03-15T11:00:00+01:00")]
		public void DateTimeOffsetText_IsTheSameInstant(string text)
		{
			// DateTimeOffset equality compares instants, so both of the first two events match.
			Assert.Equal([0, 1], EventIndexes([text]));
		}

		[Theory]
		[InlineData("2024-03-15T10:00:00")]
		[InlineData("2024-03-15")]
		public void DateTimeOffsetText_WithoutAnOffset_IsRejected(string text)
		{
			var ex = Assert.Throws<FilterConditionAndValueNotSupportedException>(() => EventIndexes([text]));

			Assert.Contains("Use ISO 8601 with Z or an offset", ex.Message);
		}
	}
}
