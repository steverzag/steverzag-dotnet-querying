using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class ChainRootTests
{
    public class Line
    {
        public int Weight { get; set; }
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public int Limit { get; set; } = 20;
        public int Days { get; set; } = 3;
        public string Name { get; set; } = "Ann";
        public string? Nickname { get; set; }
        public string Letter { get; set; } = "b";
        public DateTime Created { get; set; } = new(2024, 1, 10);
        public List<Line> Lines { get; set; } = [new() { Weight = 3 }, new() { Weight = 1 }, new() { Weight = 2 }];
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Item>)text : (PropertySelector)text;

    private static Expression Body(PropertySelector selector)
        => ExpressionUtils.CreateMemberAccess(Expression.Parameter(typeof(Item), "x"), selector);

    private static object? Evaluate(PropertySelector selector)
    {
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Item());
    }

    public static TheoryData<string, string, object?, bool> PipedAndStatic
    {
        get
        {
            var data = new TheoryData<string, string, object?, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("5|$FN_MATH_MAX(Age)", "$FN_MATH_MAX(5, Age)", 30, generic);
                data.Add("50L|$FN_MATH_MIN(Age)", "$FN_MATH_MIN(50L, Age)", 30L, generic);
                data.Add("2.0|$FN_MATH_POW(Days)", "$FN_MATH_POW(2.0, Days)", 8.0, generic);
                data.Add("\"x\"|$FN_STR_CONCAT(Name)", "$FN_STR_CONCAT(\"x\", Name)", "xAnn", generic);
                data.Add("\"Ann\"|$FN_STR_COMPARE(Name)", "$FN_STR_COMPARE(\"Ann\", Name)", 0, generic);
                data.Add("$FN_DT_DATETIME(\"2024-01-01\")|$FN_DT_ADDDAYS(Days)", "$FN_DT_ADDDAYS($FN_DT_DATETIME(\"2024-01-01\"), Days)", new DateTime(2024, 1, 4), generic);
                data.Add("$FN_DT_DATEONLY(\"2024-01-31\")|$FN_DT_ADDMONTHS(Days)", "$FN_DT_ADDMONTHS($FN_DT_DATEONLY(\"2024-01-31\"), Days)", new DateOnly(2024, 4, 30), generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PipedAndStatic))]
    public void AChainThatStartsWithALiteral_ResolvesArgumentsAgainstTheRoot(string piped, string @static, object? expected, bool generic)
    {
        var selector = Parse(piped, generic);
        var body = Body(selector);

        Assert.Equal(expected, Evaluate(selector));
        Assert.Equal(Body(Parse(@static, generic)).ToString(), body.ToString());
        if (generic || selector.Type is not null)
            Assert.Equal(body.Type, selector.Type);
    }

    public static TheoryData<string, object?, bool> Values
    {
        get
        {
            var data = new TheoryData<string, object?, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("\"abc\"|$FN_STR_CONTAINS(Letter)", true, generic);
                data.Add("\"abc\"|$FN_STR_INDEXOF(Letter)", 1, generic);
                data.Add("\"abc\"|$FN_STR_REPLACE(Letter, Name)", "aAnnc", generic);
                data.Add("\"  abc \"|$FN_STR_TRIM|$FN_STR_STARTSWITH(Letter)", false, generic);
                data.Add("\"x\"|$FN_COALESCE(Nickname)", "x", generic);
                data.Add("5|$FN_MATH_MAX(Age)|$FN_MATH_MIN(Limit)", 20, generic);
                data.Add("5|$FN_MATH_MAX(Age)|$FN_MATH_MIN(Limit)|$FN_MATH_MAX(Days)", 20, generic);
                data.Add("$FN_MATH_MAX(1, 5|$FN_MATH_MAX(Age))", 30, generic);
                data.Add("Age + 5|$FN_MATH_MAX(Age)", 60, generic);
                data.Add("(5|$FN_MATH_MAX(Age)) * 2", 60, generic);
                data.Add("5|$FN_MATH_MAX(Age + Days)", 33, generic);
                data.Add("5|$FN_MATH_MAX(\"ab\"|$FN_STR_CONCAT(Name)|$FN_STR_LENGTH)", 5, generic);
                data.Add("Lines|$FN_ORDER_ASC(10|$FN_MATH_MIN(Weight))|$FN_ELM_FIRST|Weight", 1, generic);
                data.Add("Lines|$FN_ORDER_DESC(0|$FN_MATH_MAX(Weight))|$FN_ELM_FIRST|Weight", 3, generic);
                data.Add("Created|$FN_DT_ADDDAYS(Days)|Day", 13, generic);
                data.Add("Name|$FN_STR_CONCAT(Letter)", "Annb", generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void ArgumentsResolveAgainstTheRoot_WhereverTheChainStarts(string text, object? expected, bool generic)
    {
        var selector = Parse(text, generic);

        Assert.Equal(expected, Evaluate(selector));
        if (generic || selector.Type is not null)
            Assert.Equal(Body(selector).Type, selector.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACurrentTimeFunctionAtTheHead_ReadsArgumentsFromTheRoot(bool generic)
    {
        var body = Body(Parse("$FN_DT_NOW|$FN_DT_ADDDAYS(Days)", generic));

        Assert.Equal("DateTime.Now.AddDays(Convert(x.Days, Double))", body.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheRootIsRestored_AfterEachBuild(bool generic)
    {
        var first = Body(Parse("5|$FN_MATH_MAX(Age)", generic));
        var lines = Expression.Parameter(typeof(Line), "l");
        var second = ExpressionUtils.CreateMemberAccess(lines, generic ? (PropertySelector<Line>)"5|$FN_MATH_MAX(Weight)" : (PropertySelector)"5|$FN_MATH_MAX(Weight)");

        Assert.Contains("x.Age", first.ToString());
        Assert.Contains("l.Weight", second.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUnknownArgumentAfterALiteral_IsReportedOnTheRootType(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Body(Parse("5|$FN_MATH_MAX(Missing)", generic)));

        Assert.Equal("Property 'Missing' was not found on type 'Item'.", exception.Detail);
    }
}
