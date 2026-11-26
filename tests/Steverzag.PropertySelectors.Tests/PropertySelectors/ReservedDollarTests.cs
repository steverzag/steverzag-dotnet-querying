namespace Steverzag.PropertySelectors.Tests;

public class ReservedDollarTests
{
    public class Line
    {
        public int Id { get; set; }
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public List<Line> Lines { get; set; } = [new() { Id = 4 }];
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    public static TheoryData<string, string, int, bool> Unknown
    {
        get
        {
            var data = new TheoryData<string, string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("$", "$", 0, generic);
                data.Add("$FOO", "$FOO", 0, generic);
                data.Add("$FOO(Age)", "$FOO", 0, generic);
                data.Add("$ARITH_OPERATION", "$ARITH_OPERATION", 0, generic);
                data.Add("$VAL_INT", "$VAL_INT", 0, generic);
                data.Add("$FN_NOPE", "$FN_NOPE", 0, generic);
                data.Add("Age|$FOO", "$FOO", 4, generic);
                data.Add("Lines|$FN_ELM_FIRST|$FOO", "$FOO", 20, generic);
                data.Add("$FOO|Age", "$FOO", 0, generic);
                data.Add("Age + $FOO", "$FOO", 6, generic);
                data.Add("(Age + 1)|$FOO", "$FOO", 10, generic);
                data.Add("$FN_MATH_MAX(Age, $FOO)", "$FOO", 18, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Unknown))]
    public void AnUnregisteredDollarToken_IsAnUnknownFunction(string text, string token, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal($"Unknown function '{token}'.", exception.Detail);
        Assert.Equal(token, exception.Token);
        Assert.Equal(position, exception.Position);
        Assert.Equal(text, exception.Selector);
    }

    public static TheoryData<string, string, string, int, bool> WronglyCased
    {
        get
        {
            var data = new TheoryData<string, string, string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("Name|$fn_str_trim", "$fn_str_trim", "$FN_STR_TRIM", 5, generic);
                data.Add("$fn_math_max(Age, 1)", "$fn_math_max", "$FN_MATH_MAX", 0, generic);
                data.Add("Lines|$Fn_Aggr_Count", "$Fn_Aggr_Count", "$FN_AGGR_COUNT", 6, generic);
                data.Add("Age + Name|$FN_STR_length", "$FN_STR_length", "$FN_STR_LENGTH", 11, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(WronglyCased))]
    public void AWronglyCasedToken_IsUnknown_AndNamesTheRegisteredOne(string text, string token, string registered, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal($"Unknown function '{token}'. Function names are case-sensitive: '{registered}'.", exception.Detail);
        Assert.Equal(token, exception.Token);
        Assert.Equal(position, exception.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMemberAfterAnUnknownToken_IsStillAnUnknownFunction(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse("$FOO.Age", generic));

        Assert.Equal("Unknown function '$FOO.Age'.", exception.Detail);
    }
}
