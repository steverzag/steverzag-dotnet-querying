using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class LiteralAfterPipeTests
{
    public class Inner
    {
        public int Value { get; set; } = 9;
        public int True { get; set; } = 6;
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public Inner Inner { get; set; } = new();
    }

    private const string Detail = "A literal can't follow '|': it would discard the value before it.";

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    private static object? Evaluate(PropertySelector selector)
    {
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Item());
    }

    public static TheoryData<string, string, int, bool> Rejected
    {
        get
        {
            var data = new TheoryData<string, string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("Age|5", "5", 4, generic);
                data.Add("Age | 5", "5", 6, generic);
                data.Add("Name|\"x\"", "\"x\"", 5, generic);
                data.Add("Age|true", "true", 4, generic);
                data.Add("Age|1.5m", "1.5m", 4, generic);
                data.Add("Age|5|$FN_MATH_SQRT", "5", 4, generic);
                data.Add("Inner|Value|2", "2", 12, generic);
                data.Add("5|7", "7", 2, generic);
                data.Add("(Age + 1)|5", "5", 10, generic);
                data.Add("Age + Name|\"x\"", "\"x\"", 11, generic);
                data.Add("$FN_MATH_MAX(Age|2, 1)", "2", 17, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void ALiteralAfterAPipe_IsRejected_AtItsPosition(string text, string token, int position, bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

        Assert.Equal(Detail, exception.Detail);
        Assert.Equal(token, exception.Token);
        Assert.Equal(position, exception.Position);
        Assert.Equal(text, exception.Selector);
    }

    public static TheoryData<string, object, bool> Allowed
    {
        get
        {
            var data = new TheoryData<string, object, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("5|$FN_MATH_SQRT", Math.Sqrt(5), generic);
                data.Add("\"ab\"|$FN_STR_LENGTH", 2, generic);
                data.Add("true", true, generic);
                data.Add("Age + 2|$FN_MATH_POW(2)", 34d, generic);
                data.Add("$FN_MATH_MAX(4|$FN_MATH_SQRT, 1)", 2d, generic);
                data.Add("Age|$FN_MATH_MAX(5)", 30, generic);
                data.Add("Inner|@True", 6, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Allowed))]
    public void ALiteralAtTheHead_OrAsAnArgument_IsStillAllowed(string text, object expected, bool generic)
    {
        Assert.Equal(expected, Evaluate(Parse(text, generic)));
    }
}
