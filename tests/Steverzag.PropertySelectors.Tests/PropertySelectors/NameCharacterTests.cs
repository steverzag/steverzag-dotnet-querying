using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class NameCharacterTests
{
    public class Inner
    {
        public int Value { get; set; } = 7;
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public Inner Inner { get; set; } = new();
        public int _x { get; set; } = 1;
        public int Né { get; set; } = 2;
        public int X1 { get; set; } = 3;
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    private const string Whitespace = "A property name can't contain whitespace.";
    private const string Paren = "'(' can't follow a property name: only a function ('$FN_…') takes arguments.";
    private const string Empty = "Empty property name: '.' must sit between two names.";
    private const string DigitStart = "A property name can't start with a digit.";

    private static string Unexpected(char c) => $"Unexpected character '{c}' in a property name.";

    public static TheoryData<string, string, int, string, bool> Rejected
    {
        get
        {
            var data = new TheoryData<string, string, int, string, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("Age # 2", Whitespace, 3, " ", generic);
                data.Add("Inner . Value", Whitespace, 5, " ", generic);
                data.Add("Age#", Unexpected('#'), 3, "#", generic);
                data.Add("Na$me", Unexpected('$'), 2, "$", generic);
                data.Add("Inner.$Value", Unexpected('$'), 6, "$", generic);
                data.Add("A@b", Unexpected('@'), 1, "@", generic);
                data.Add("Age()", Paren, 3, "(", generic);
                data.Add("Inner.Value(1)", Paren, 11, "(", generic);
                data.Add("Inner.1st", DigitStart, 6, "1st", generic);
                data.Add(".Age", Empty, 0, ".", generic);
                data.Add("Inner.", Empty, 5, ".", generic);
                data.Add("Age..Age", Empty, 4, ".", generic);
                data.Add("Age|Na#me", Unexpected('#'), 6, "#", generic);
                data.Add("Age + A#b", Unexpected('#'), 7, "#", generic);
                data.Add("$FN_MATH_MAX(Age, A#b)", Unexpected('#'), 19, "#", generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void ANameThatIsntAnIdentifier_IsRejectedWhenParsed(string text, string detail, int position, string token, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal(detail, exception.Detail);
        Assert.Equal(position, exception.Position);
        Assert.Equal(token, exception.Token);
        Assert.Equal(text, exception.Selector);
    }

    public static TheoryData<string, string, int, bool> MalformedNumbers
    {
        get
        {
            var data = new TheoryData<string, string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("1.", "'1.' isn't a valid number. A '.' needs a digit on both sides ('0.5', '1.0').", 0, generic);
                data.Add(".5", "'.5' isn't a valid number. A '.' needs a digit on both sides ('0.5', '1.0').", 0, generic);
                data.Add("1e", "'1e' isn't a valid number. An exponent needs digits after 'e' ('1e3').", 0, generic);
                data.Add("0x10", "'0x10' isn't a valid number.", 0, generic);
                data.Add("1_000", "'1_000' isn't a valid number.", 0, generic);
                data.Add("1abc", "'1abc' isn't a valid number.", 0, generic);
                data.Add("Age|1x", "'1x' isn't a valid number.", 4, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MalformedNumbers))]
    public void APartThatStartsLikeANumber_IsAMalformedNumber(string text, string detail, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal(detail, exception.Detail);
        Assert.Equal(position, exception.Position);
    }

    [Theory]
    [InlineData("_x", 1, true)]
    [InlineData("_x", 1, false)]
    [InlineData("Né", 2, true)]
    [InlineData("Né", 2, false)]
    [InlineData("X1", 3, true)]
    [InlineData("X1", 3, false)]
    [InlineData("@Inner.@Value", 7, true)]
    [InlineData("@Inner.@Value", 7, false)]
    [InlineData("  Age  ", 30, true)]
    [InlineData("  Age  ", 30, false)]
    public void AnIdentifier_IsAccepted(string text, int expected, bool generic)
    {
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, Parse(text, generic));

        Assert.Equal(expected, Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Item()));
    }
}
