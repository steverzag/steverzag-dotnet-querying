namespace Steverzag.PropertySelectors.Tests;

public class FunctionSpaceTests
{
    public class Item
    {
        public int Age { get; set; } = 30;
        public double Price { get; set; } = 1.25;
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    public static TheoryData<string, string, int, string, bool> Rejected
    {
        get
        {
            var data = new TheoryData<string, string, int, string, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("$FN_MATH_MAX (Age, 1)", "$FN_MATH_MAX", 12, " ", generic);
                data.Add("$FN_MATH_MAX   (Age, 1)", "$FN_MATH_MAX", 12, "   ", generic);
                data.Add("Price|$FN_MATH_ROUND (2)", "$FN_MATH_ROUND", 20, " ", generic);
                data.Add("$FN_MATH_MAX(Age, $FN_MATH_MIN (Age, 1))", "$FN_MATH_MIN", 30, " ", generic);
                data.Add("$FN_MATH_MAX (Age, 1) + 1", "$FN_MATH_MAX", 12, " ", generic);
                data.Add("$FOO (1)", "$FOO", 4, " ", generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void ASpaceBeforeTheArguments_IsRejected_AtTheSpace(string text, string function, int position, string token, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal($"No space is allowed between a function and its '(': write '{function}('.", exception.Detail);
        Assert.Equal(position, exception.Position);
        Assert.Equal(token, exception.Token);
        Assert.Equal(text, exception.Selector);
    }

    [Theory]
    [InlineData("$FN_MATH_MAX(Age, 1)", true)]
    [InlineData("$FN_MATH_MAX(Age, 1)", false)]
    [InlineData("Price|$FN_MATH_ROUND ", true)]
    [InlineData("Price|$FN_MATH_ROUND ", false)]
    [InlineData("Price | $FN_MATH_ROUND(1) | $FN_MATH_SQRT", true)]
    [InlineData("Price | $FN_MATH_ROUND(1) | $FN_MATH_SQRT", false)]
    [InlineData("$FN_MATH_MAX(Age, 1) + 1", true)]
    [InlineData("$FN_MATH_MAX(Age, 1) + 1", false)]
    public void WithoutASpaceBeforeTheArguments_ItParses(string text, bool generic)
    {
        Parse(text, generic);
    }
}
