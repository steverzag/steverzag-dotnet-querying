using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ArithmeticSelectors;

public class MathMaxMinTests
{
    public class Item
    {
        public int Small { get; set; } = 3;
        public int Large { get; set; } = 5;
        public long Views { get; set; } = 9;
        public decimal Price { get; set; } = 0.5015m;
        public decimal Cost { get; set; } = 0.5014m;
        public double Ratio { get; set; } = 0.25;
        public int? MaybeSmall { get; set; }
        public int? MaybeLarge { get; set; }
        public decimal? MaybePrice { get; set; }
    }

    private static Expression Body(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(Item), "x");
        return SelectorExpressions.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate(PropertySelector selector, Item item)
    {
        var body = Body(selector, out var parameter);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(item);
    }

    private static IEnumerable<PropertySelector> BothForms(string text)
        => [(PropertySelector<Item>)text, (PropertySelector)text];

    // ── Result type: the operands' common type, nullable only when both are ─

    [Theory]
    [InlineData("$FN_MATH_MAX(Small, Large)", typeof(int))]
    [InlineData("$FN_MATH_MAX(Small, Views)", typeof(long))]
    [InlineData("$FN_MATH_MAX(Price, Small)", typeof(decimal))]
    [InlineData("$FN_MATH_MAX(Price, Ratio)", typeof(decimal))]
    [InlineData("$FN_MATH_MAX(Ratio, 1)", typeof(double))]
    [InlineData("$FN_MATH_MAX(MaybeSmall, Large)", typeof(int))]
    [InlineData("$FN_MATH_MAX(Large, MaybeSmall)", typeof(int))]
    [InlineData("$FN_MATH_MAX(MaybeSmall, MaybeLarge)", typeof(int?))]
    [InlineData("$FN_MATH_MAX(MaybePrice, Price)", typeof(decimal))]
    [InlineData("$FN_MATH_MIN(MaybeSmall, MaybeLarge)", typeof(int?))]
    [InlineData("$FN_MATH_MIN(Price, Ratio)", typeof(decimal))]
    [InlineData("Price|$FN_MATH_MAX(Cost)", typeof(decimal))]
    public void DeclaresTheTypeItBuilds(string text, Type expected)
    {
        PropertySelector<Item> selector = text;

        Assert.Equal(expected, selector.Type);
        Assert.Equal(expected, Body(selector, out _).Type);
        Assert.Equal(expected, Body((PropertySelector)text, out _).Type);
    }

    // ── Values ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("$FN_MATH_MAX(Small, Large)", 5)]
    [InlineData("$FN_MATH_MAX(Large, Small)", 5)]
    [InlineData("$FN_MATH_MIN(Small, Large)", 3)]
    [InlineData("$FN_MATH_MIN(Large, Small)", 3)]
    [InlineData("Small|$FN_MATH_MAX(Large)", 5)]
    [InlineData("Large|$FN_MATH_MIN(Small)", 3)]
    public void PicksTheLargerOrSmaller(string text, int expected)
    {
        foreach (var selector in BothForms(text))
            Assert.Equal(expected, Evaluate(selector, new Item()));
    }

    [Fact]
    public void DecimalsAreExact()
    {
        foreach (var selector in BothForms("Price|$FN_MATH_MAX(Cost)"))
            Assert.Equal(0.5015m, Evaluate(selector, new Item()));

        foreach (var selector in BothForms("Price|$FN_MATH_MIN(Cost)"))
            Assert.Equal(0.5014m, Evaluate(selector, new Item()));
    }

    // ── Nulls are ignored; the result is null only when both are ─────────

    [Theory]
    [InlineData(null, 5, 5, 5)]
    [InlineData(5, null, 5, 5)]
    [InlineData(null, null, null, null)]
    [InlineData(3, 5, 5, 3)]
    [InlineData(5, 3, 5, 3)]
    public void BothNullable_IgnoresANull(int? a, int? b, int? max, int? min)
    {
        var item = new Item { MaybeSmall = a, MaybeLarge = b };

        foreach (var selector in BothForms("$FN_MATH_MAX(MaybeSmall, MaybeLarge)"))
            Assert.Equal(max, Evaluate(selector, item));

        foreach (var selector in BothForms("$FN_MATH_MIN(MaybeSmall, MaybeLarge)"))
            Assert.Equal(min, Evaluate(selector, item));
    }

    [Theory]
    [InlineData(null, 5, 3)]
    [InlineData(7, 7, 3)]
    [InlineData(1, 5, 1)]
    public void OneNullable_GivesTheOtherForANull(int? a, int max, int min)
    {
        var item = new Item { MaybeSmall = a };

        foreach (var selector in BothForms("MaybeSmall|$FN_MATH_MAX(Large)"))
            Assert.Equal(max, Evaluate(selector, item));

        foreach (var selector in BothForms("$FN_MATH_MIN(Small, MaybeSmall)"))
            Assert.Equal(min, Evaluate(selector, item));
    }

    [Fact]
    public void DoesNotCallMath()
    {
        var body = Body((PropertySelector<Item>)"$FN_MATH_MAX(Small, Large)", out _);

        Assert.IsAssignableFrom<ConditionalExpression>(body);
    }
}
