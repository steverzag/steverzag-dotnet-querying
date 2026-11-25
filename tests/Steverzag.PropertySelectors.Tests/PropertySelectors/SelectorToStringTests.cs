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
    }

    private static readonly TestItem Sample = new()
    {
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
            "Quantity * $VAL_DOUBLE:1.5",
            "$FN_MATH_POW(Quantity + LongValue, Quantity)",
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
    [InlineData("$VAL_STR:n/a", "\"n/a\"")]
    [InlineData("$VAL_STR:say \"hi\"", "$VAL_STR:say \"hi\"")]
    [InlineData("$VAL_INT:42", "42")]
    [InlineData("$VAL_INT:-5", "$VAL_INT:-5")]
    [InlineData("$VAL_BOOL:True", "true")]
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
    public void ToGeneric_KeepsTheWholeSelector(string selector)
    {
        var generic = ((PropertySelector)selector).ToGeneric<TestItem>();

        Assert.Equal(selector, generic.ToString());
        Assert.Equal(Evaluate((PropertySelector)selector, Sample), Evaluate(generic, Sample));
    }
}
