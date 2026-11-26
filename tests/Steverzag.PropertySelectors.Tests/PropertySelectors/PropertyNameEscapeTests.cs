using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class PropertyNameEscapeTests
{
    public class Inner
    {
        public int True { get; set; } = 6;
    }

    public class Item
    {
        public int True { get; set; } = 5;
        public string False { get; set; } = "no";
        public int Age { get; set; } = 30;
        public Inner Inner { get; set; } = new();
        public List<Inner> Items { get; set; } = [new()];
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    private static object? Evaluate(PropertySelector selector, out Type built)
    {
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);

        built = body.Type;
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Item());
    }

    public static TheoryData<string, object, Type, bool> Escaped
    {
        get
        {
            var data = new TheoryData<string, object, Type, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("@True", 5, typeof(int), generic);
                data.Add("@False", "no", typeof(string), generic);
                data.Add("@Age", 30, typeof(int), generic);
                data.Add("Inner.@True", 6, typeof(int), generic);
                data.Add("@Inner.@True", 6, typeof(int), generic);
                data.Add("Inner|@True", 6, typeof(int), generic);
                data.Add("Items|$FN_ELM_FIRST|@True", 6, typeof(int), generic);
                data.Add("@False|$FN_STR_LENGTH", 2, typeof(int), generic);
                data.Add("@True + 1", 6, typeof(int), generic);
                data.Add("(@True + 1) * 2", 12, typeof(int), generic);
                data.Add("$FN_MATH_MAX(@True, 1)", 5, typeof(int), generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Escaped))]
    public void AnEscapedName_IsAProperty(string text, object expected, Type type, bool generic)
    {
        var selector = Parse(text, generic);

        Assert.Equal(expected, Evaluate(selector, out var built));
        Assert.Equal(type, built);
        if (generic)
            Assert.Equal(type, selector.Type);
        Assert.Equal(text, selector.ToString());
    }

    [Theory]
    [InlineData("True", true, true)]
    [InlineData("True", true, false)]
    [InlineData("false", false, true)]
    [InlineData("false", false, false)]
    public void AnUnescapedName_IsStillALiteral(string text, bool expected, bool generic)
    {
        Assert.Equal(expected, Evaluate(Parse(text, generic), out var built));
        Assert.Equal(typeof(bool), built);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EscapedAndPlainNames_BuildTheSameExpression(bool generic)
    {
        var parameter = Expression.Parameter(typeof(Item), "x");

        var escaped = ExpressionUtils.CreateMemberAccess(parameter, Parse("@Inner.@True", generic));
        var plain = ExpressionUtils.CreateMemberAccess(parameter, Parse("Inner.True", generic));

        Assert.Equal(plain.ToString(), escaped.ToString());
    }

    public static TheoryData<string, int, bool> Dangling
    {
        get
        {
            var data = new TheoryData<string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("@", 0, generic);
                data.Add("@@True", 0, generic);
                data.Add("Inner.@", 6, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Dangling))]
    public void AnEscapeWithoutAName_IsRejected(string text, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Evaluate(Parse(text, generic), out _));

        Assert.Equal("'@' must be followed by a property name.", exception.Detail);
        Assert.Equal("@", exception.Token);
        Assert.Equal(position, exception.Position);
    }

    public static TheoryData<string, string, string, int, bool> Unknown
    {
        get
        {
            var data = new TheoryData<string, string, string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("@Missing", "Missing", "Property 'Missing' was not found on type 'Item'.", 1, generic);
                data.Add("Inner.@Missing", "Missing", "Property 'Missing' was not found on type 'Inner'.", 7, generic);
                data.Add("Age|@Missing", "Missing", "Property 'Missing' was not found on type 'Int32'.", generic ? 5 : 1, generic);
                data.Add("@true", "true", "Property 'true' was not found on type 'Item'. Property names are case-sensitive: 'True'.", 1, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Unknown))]
    public void AnEscapedUnknownName_ReportsTheNameWithoutTheEscape(string text, string token, string detail, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Evaluate(Parse(text, generic), out _));

        Assert.Equal(detail, exception.Detail);
        Assert.Equal(token, exception.Token);
        Assert.Equal(position, exception.Position);
    }
}
