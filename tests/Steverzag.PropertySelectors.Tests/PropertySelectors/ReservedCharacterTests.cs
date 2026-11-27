using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class ReservedCharacterTests
{
    public class Line
    {
        public int Id { get; set; }
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public TimeOnly Opens { get; set; } = new(8, 30);
        public List<Line> Lines { get; set; } = [new() { Id = 4 }];
    }

    private const string Detail = "':' is reserved; write it inside a quoted string to use it as text.";

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    public static TheoryData<string, int, bool> Rejected
    {
        get
        {
            var data = new TheoryData<string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add(":", 0, generic);
                data.Add("Age:a", 3, generic);
                data.Add(":a|Age", 0, generic);
                data.Add("Lines|$FN_ELM_FIRST:a|Id", 19, generic);
                data.Add("Lines|$FN_ELM_FIRST:a|Id|$FN_MATH_MIN(:a|Id - 1)", 19, generic);
                data.Add("Age|$FN_MATH_MIN(:a|Age)", 17, generic);
                data.Add("$FN_MATH_MAX(Age, $FN_MATH_MIN(Age, :b))", 36, generic);
                data.Add("Age + Name:x|$FN_STR_LENGTH", 10, generic);
                data.Add("Age * 2:x", 7, generic);
                data.Add("Age + : 1", 6, generic);
                data.Add("(Age:x + 1) * 2", 4, generic);
                data.Add("(Age + 1)|$FN_MATH_ROUND:r", 24, generic);
                data.Add("$VAL_INT:5", 8, generic);
                data.Add("Name|$FN_STR_CONCAT(\"a\", :x)", 25, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void AnUnquotedColon_IsRejected_AtItsPosition(string text, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal(Detail, exception.Detail);
        Assert.Equal(position, exception.Position);
        Assert.Equal(":", exception.Token);
        Assert.Equal(text, exception.Selector);
    }

    public static TheoryData<string, object?, bool> QuotedColons
    {
        get
        {
            var data = new TheoryData<string, object?, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("\"a:b\"", "a:b", generic);
                data.Add("Name|$FN_STR_CONCAT(\":\")", "Ann:", generic);
                data.Add("Age + \"1:2\"|$FN_STR_LENGTH", 33, generic);
                data.Add("$FN_DT_TIMEONLY(\"08:30\")", new TimeOnly(8, 30), generic);
                data.Add("$FN_DT_DATETIME(\"2024-01-01T10:15:00\")|Minute", 15, generic);
                data.Add("Name|$FN_STR_REPLACE(\"n\", \"::\")", "A::::", generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(QuotedColons))]
    public void AColonInsideAQuotedString_IsText(string text, object? expected, bool generic)
    {
        var selector = Parse(text, generic);
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = SelectorExpressions.CreateMemberAccess(parameter, selector);

        Assert.Equal(expected, Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Item()));
        Assert.Equal(text, selector.ToString());
    }

    [Fact]
    public void TheReservedCharacter_IsAConstant()
    {
        Assert.Equal(':', SelectorDefaults.ReservedCharacter);
    }

    [Fact]
    public void SelectorLiteralFormat_QuotesAColon()
    {
        var text = SelectorLiteral.Format("a:b");

        Assert.Equal("\"a:b\"", text);
        Assert.Equal("a:b", Expression.Lambda(SelectorExpressions.CreateMemberAccess(Expression.Parameter(typeof(Item), "x"), (PropertySelector)text), Expression.Parameter(typeof(Item), "x")).Compile().DynamicInvoke(new Item()));
    }
}
