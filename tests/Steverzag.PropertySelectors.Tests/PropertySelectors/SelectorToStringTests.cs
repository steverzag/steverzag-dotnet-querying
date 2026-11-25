using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.Extensions;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class SelectorToStringTests
{
    private class Inner
    {
        public int Value { get; set; }
    }

    private class TestItem
    {
        public int Quantity { get; set; }
        public long LongValue { get; set; }
        public int? First { get; set; }
        public int? Second { get; set; }
        public int? Third { get; set; }
        public string? Nickname { get; set; }
        public string Name { get; set; } = string.Empty;
        public Inner Inner { get; set; } = new();
        public List<int> Numbers { get; set; } = [];
        public DateTime Created { get; set; }
    }

    private static readonly TestItem Sample = new()
    {
        Created = new DateTime(2024, 3, 15),
        Quantity = 7,
        LongValue = 3,
        First = null,
        Second = 5,
        Third = 9,
        Nickname = null,
        Name = "hello",
        Inner = new Inner { Value = 4 },
        Numbers = [2, 8, 6],
    };

    private static object? Evaluate(PropertySelector selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = Expression.Convert(ExpressionUtils.CreateMemberAccess(parameter, selector), typeof(object));
        return Expression.Lambda<Func<TestItem, object?>>(body, parameter).Compile()(item);
    }

    public static TheoryData<bool> BothForms() => new() { true, false };

    private static PropertySelector Parse(string selector, bool generic)
        => generic ? (PropertySelector<TestItem>)selector : (PropertySelector)selector;

    public static TheoryData<string, bool> Canonical()
    {
        string[] selectors =
        [
            "Name",
            "Inner.Value",
            "Numbers|$FN_AGGR_MAX",
            "Numbers|$FN_AGGR_MAX|$FN_COALESCE(0)",
            "First|$FN_COALESCE(Second, Third)",
            "First|$FN_COALESCE(Numbers|$FN_AGGR_MAX)",
            "Nickname|$FN_COALESCE(\"n/a\")",
            "Name|$FN_STR_REPLACE(\"l\", \"-\")",
            "Name|$FN_STR_CONTAINS(\"a|b\")",
            "$FN_STR_CONCAT(\"(\", Name, \")\")",
            "Quantity + LongValue",
            "Quantity * LongValue + Quantity",
            "(Quantity + LongValue) * Quantity",
            "Quantity - LongValue - Quantity",
            "Quantity - (LongValue - Quantity)",
            "Quantity / (LongValue * Quantity)",
            "-Quantity",
            "-(Quantity + LongValue)",
            "Quantity * -LongValue",
            "Numbers|$FN_AGGR_MAX + Quantity",
            "First|$FN_COALESCE(Second) + 1",
            "Quantity * -2",
            "Quantity * 1.5m",
            "Quantity * 1.5",
            "-2.5 * Quantity - 1",
            "$FN_MATH_POW(Quantity + LongValue, Quantity)",
            "$FN_MATH_MAX(Quantity, 1)",
            "Quantity|$FN_MATH_ROUND",
            "Quantity|$FN_MATH_MAX(LongValue)",
            "Inner.Value|$FN_MATH_POW(2)",
            "Name|$FN_STR_CONCAT(\"-\", Nickname)",
            "Name|$FN_STR_TRIM|$FN_STR_CONCAT(\"!\")",
            "Numbers|$FN_AGGR_SUM|$FN_MATH_SQRT",
            "Created|$FN_DT_ADDDAYS(-1)",
            "Created|$FN_DT_ADDDAYS(Quantity)|$FN_DT_ADDHOURS(2)",
            "Quantity|$FN_MATH_MAX(1) * 2",
            "(Quantity|$FN_MATH_MAX(1) + 2) * 3",
            "Quantity|$FN_MATH_MAX(First|$FN_COALESCE(0))",
            "First|$FN_COALESCE(Quantity|$FN_MATH_MAX(1))",
        ];

        var data = new TheoryData<string, bool>();
        foreach (var selector in selectors)
        {
            data.Add(selector, true);
            data.Add(selector, false);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Canonical))]
    public void CanonicalText_IsReturnedUnchanged(string selector, bool generic)
    {
        Assert.Equal(selector, Parse(selector, generic).ToString());
    }

    [Theory]
    [InlineData("Name|$FN_STR_REPLACE(\"l\",\"-\")", "Name|$FN_STR_REPLACE(\"l\", \"-\")")]
    [InlineData("(Quantity * LongValue) + Quantity", "Quantity * LongValue + Quantity")]
    [InlineData("\"n/a\"", "\"n/a\"")]
    [InlineData("TRUE", "true")]
    [InlineData("2.50", "2.5")]
    [InlineData("5l", "5L")]
    [InlineData("1e3", "1000.0")]
    [InlineData("Quantity * -(2)", "Quantity * -2")]
    [InlineData("Name|$FN_STR_CONCAT(\"-\",Nickname)", "Name|$FN_STR_CONCAT(\"-\", Nickname)")]
    [InlineData("Quantity|$FN_MATH_ROUND()", "Quantity|$FN_MATH_ROUND")]
    [InlineData("(Quantity|$FN_MATH_MAX(1)) * 2", "Quantity|$FN_MATH_MAX(1) * 2")]
    public void EquivalentText_IsNormalized(string selector, string expected)
    {
        Assert.Equal(expected, ((PropertySelector<TestItem>)selector).ToString());
        Assert.Equal(expected, ((PropertySelector)selector).ToString());
    }

    [Theory]
    [InlineData("Numbers|$FN_AGGR_SUM")]
    [InlineData("Numbers|$FN_AGGR_MAX|$FN_COALESCE(0) * 2")]
    [InlineData("First|$FN_COALESCE(Second, Third, 0)")]
    [InlineData("Nickname|$FN_COALESCE(\"a, (b)|c\")")]
    [InlineData("$FN_MATH_POW(2, 3) * 2")]
    [InlineData("Quantity * -2")]
    [InlineData("-(Quantity - 10) / 2")]
    [InlineData("10 - (Quantity - LongValue)")]
    [InlineData("Quantity - -LongValue")]
    [InlineData("-(-Quantity)")]
    [InlineData("Name|$FN_STR_INDEXOF(\"l\") + Inner.Value")]
    [InlineData("-1.5 * Quantity + 2.5")]
    [InlineData("-(Quantity - 1.5) / 2.0")]
    [InlineData("LongValue * 3L - 1")]
    [InlineData("Nickname|$FN_COALESCE(\"say \"\"hi\"\", (a|b)\")")]
    [InlineData("Quantity|$FN_MATH_MAX(LongValue) * 2")]
    [InlineData("Numbers|$FN_AGGR_SUM|$FN_MATH_SQRT")]
    [InlineData("Name|$FN_STR_TRIM|$FN_STR_CONCAT(\"!\")")]
    [InlineData("Quantity|$FN_MATH_MAX(First|$FN_COALESCE(0))")]
    [InlineData("Created|$FN_DT_ADDDAYS(-1)")]
    public void ReparsingTheText_GivesTheSameValue(string selector)
    {
        foreach (var generic in new[] { true, false })
        {
            var original = Parse(selector, generic);
            var text = original.ToString();

            foreach (var reparseGeneric in new[] { true, false })
            {
                var reparsed = Parse(text, reparseGeneric);

                Assert.Equal(Evaluate(original, Sample), Evaluate(reparsed, Sample));
                Assert.Equal(text, reparsed.ToString());
            }
        }
    }

    [Theory]
    [InlineData("Inner.Value")]
    [InlineData("Numbers|$FN_AGGR_MAX|$FN_COALESCE(0)")]
    [InlineData("First|$FN_COALESCE(Second, Third)")]
    [InlineData("(Quantity + LongValue) * Quantity")]
    [InlineData("Quantity|$FN_MATH_MAX(LongValue)")]
    [InlineData("Numbers|$FN_AGGR_SUM|$FN_MATH_SQRT")]
    [InlineData("Created|$FN_DT_ADDDAYS(Quantity)")]
    public void ToGeneric_KeepsTheWholeSelector(string selector)
    {
        var generic = ((PropertySelector)selector).ToGeneric<TestItem>();

        Assert.Equal(selector, generic.ToString());
        Assert.Equal(Evaluate((PropertySelector)selector, Sample), Evaluate(generic, Sample));
    }
}
