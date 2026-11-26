using Steverzag.QueryShaper.Filtering;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class FilterValueTests
	{
		public static TheoryData<string, FilterValueKind> Kinds => new()
		{
			{ "bool", FilterValueKind.Bool },
			{ "byte", FilterValueKind.Integer },
			{ "short", FilterValueKind.Integer },
			{ "int", FilterValueKind.Integer },
			{ "long", FilterValueKind.Integer },
			{ "float", FilterValueKind.Double },
			{ "double", FilterValueKind.Double },
			{ "decimal", FilterValueKind.Decimal },
			{ "char", FilterValueKind.Char },
			{ "string", FilterValueKind.String },
			{ "Guid", FilterValueKind.Guid },
			{ "DateTime", FilterValueKind.DateTime },
			{ "DateOnly", FilterValueKind.DateOnly },
			{ "TimeOnly", FilterValueKind.TimeOnly },
			{ "TimeSpan", FilterValueKind.TimeSpan },
			{ "DateTimeOffset", FilterValueKind.DateTimeOffset },
			{ "List", FilterValueKind.Enumerable },
		};

		private static FilterValue Create(string type) => type switch
		{
			"bool" => true,
			"byte" => (byte)1,
			"short" => (short)1,
			"int" => 1,
			"long" => 1L,
			"float" => 1f,
			"double" => 1d,
			"decimal" => 1m,
			"char" => 'a',
			"string" => "a",
			"Guid" => Guid.NewGuid(),
			"DateTime" => DateTime.Now,
			"DateOnly" => DateOnly.MinValue,
			"TimeOnly" => TimeOnly.MinValue,
			"TimeSpan" => TimeSpan.Zero,
			"DateTimeOffset" => DateTimeOffset.Now,
			"List" => new List<object> { 1 },
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};

		[Theory]
		[MemberData(nameof(Kinds))]
		public void ReportsTheKindOfItsValue(string type, FilterValueKind expected)
		{
			Assert.Equal(expected, Create(type).GetKind());
		}

		[Fact]
		public void EveryKind_IsProduced()
		{
			var produced = Kinds.Select(row => (FilterValueKind)row[1]).ToHashSet();

			Assert.Equal(Enum.GetValues<FilterValueKind>().ToHashSet(), produced);
		}

		[Fact]
		public void AValidGuidString_CastsToAGuid()
		{
			var guid = Guid.NewGuid();
			FilterValue value = guid.ToString();

			Assert.Equal(guid, (Guid)value);
			Assert.Equal(guid, (Guid?)value);
		}

		[Theory]
		[InlineData("not-a-guid")]
		[InlineData("")]
		public void AnInvalidGuidString_Throws(string text)
		{
			FilterValue value = text;

			Assert.Throws<FormatException>(() => (Guid)value);
			Assert.Throws<FormatException>(() => (Guid?)value);
		}

		[Fact]
		public void ANullString_CastsToANullGuid()
		{
			FilterValue value = (string)null!;

			Assert.Null((Guid?)value);
		}
	}
}
