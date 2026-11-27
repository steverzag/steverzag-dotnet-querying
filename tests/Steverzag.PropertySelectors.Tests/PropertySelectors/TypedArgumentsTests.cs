using System.Linq.Expressions;
using Steverzag.PropertySelectors.Extensions;

namespace Steverzag.PropertySelectors.Tests;

public class TypedArgumentsTests
{
    public class Order
    {
        public DateTime Created { get; set; } = new(2024, 1, 10);
        public DateOnly Day { get; set; } = new(2024, 1, 10);
        public DateTimeOffset Stamp { get; set; } = new(2024, 1, 10, 8, 0, 0, TimeSpan.FromHours(2));
        public int Quantity { get; set; } = 4;
        public long Views { get; set; } = 9;
        public decimal Price { get; set; } = 2.5m;
        public DateTime? Deleted { get; set; }
        public int? Maybe { get; set; }
        public int? Other { get; set; } = 7;
        public double Ratio { get; set; } = 1.26;
        public List<Line> Lines { get; set; } =
        [
            new() { Weight = 3, Bonus = 0 },
            new() { Weight = 1, Bonus = 5 },
            new() { Weight = 2, Bonus = 0 },
        ];
    }

    public class Line
    {
        public int Weight { get; set; }
        public int Bonus { get; set; }
    }

    private static Expression Body(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(Order), "o");
        return SelectorExpressions.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate(PropertySelector selector, Order order)
    {
        var body = Body(selector, out var parameter);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(order);
    }

    // The function's generic argument is the piped type, so read Parameters without naming it.
    private static IReadOnlyList<PropertySelector> Arguments(PropertySelector function) =>
        (IReadOnlyList<PropertySelector>)function.GetType().GetProperty("Parameters")!.GetValue(function)!;

    // ── Declared type = built type, static form ─────────────────────────────

    public static TheoryData<string> StaticForms => new()
    {
        "$FN_DT_ADDDAYS(Created, 3)",
        "$FN_DT_ADDDAYS(Day, 3)",
        "$FN_DT_ADDDAYS(Stamp, 3)",
        "$FN_DT_ADDMONTHS(Day, Quantity)",
        "$FN_MATH_MAX(Quantity, Views)",
        "$FN_MATH_MIN(Quantity, Price)",
        "$FN_MATH_ROUND(Price, 1)",
        "$FN_MATH_ROUND(Quantity)",
        "$FN_MATH_CEILING(Ratio)",
        "$FN_MATH_MAX($FN_MATH_ROUND(Ratio), Quantity)",
        "$FN_MATH_MAX(Quantity * 2, Views)",
        "$FN_MATH_MAX(Maybe|$FN_COALESCE(0), 1)",
        "Maybe|$FN_COALESCE(Other)",
        "Maybe|$FN_COALESCE(Other, Quantity)",
        "Maybe|$FN_COALESCE(Other|$FN_COALESCE(Quantity))",
        "Deleted|$FN_COALESCE(Created)|$FN_DT_ADDDAYS(1)",
    };

    [Theory]
    [MemberData(nameof(StaticForms))]
    public void GenericForm_DeclaresTheTypeItBuilds(string text)
    {
        PropertySelector<Order> selector = text;

        Assert.NotNull(selector.Type);
        Assert.Equal(Body(selector, out _).Type, selector.Type);
    }

    [Theory]
    [InlineData("$FN_DT_ADDDAYS(Day, 3)", typeof(DateOnly))]
    [InlineData("$FN_DT_ADDDAYS(Stamp, 3)", typeof(DateTimeOffset))]
    [InlineData("$FN_MATH_MAX(Quantity, Views)", typeof(long))]
    [InlineData("Maybe|$FN_COALESCE(Other)", typeof(int?))]
    [InlineData("Maybe|$FN_COALESCE(Other, Quantity)", typeof(int))]
    public void GenericForm_DeclaresTheArgumentDrivenType(string text, Type expected)
    {
        PropertySelector<Order> selector = text;

        Assert.Equal(expected, selector.Type);
    }

    [Fact]
    public void PropertyArguments_CarryTheirType()
    {
        PropertySelector<Order> selector = "$FN_MATH_MAX(Quantity, Views)";

        Assert.Equal([typeof(int), typeof(long)], Arguments(selector).Select(a => a.Type));
    }

    [Fact]
    public void NonGenericForm_StaysUntypedUntilBuilt()
    {
        PropertySelector selector = "$FN_DT_ADDDAYS(Day, 3)";

        Assert.Null(selector.Type);
        Assert.Equal(typeof(DateOnly), Body(selector, out _).Type);
    }

    [Fact]
    public void ToGeneric_TypesTheArguments()
    {
        PropertySelector selector = "$FN_DT_ADDDAYS(Day, 3)";

        Assert.Equal(typeof(DateOnly), selector.ToGeneric<Order>().Type);
    }

    // ── A function chained after an argument-typed one ──────────────────────

    [Fact]
    public void FunctionAfterCoalesceWithAPropertyFallback_Works()
    {
        PropertySelector<Order> selector = "Maybe|$FN_COALESCE(Other)|$FN_MATH_MAX(5)";

        Assert.Equal(typeof(int?), Arguments(selector.ChildSelector!.ChildSelector!)[0].Type);
        Assert.Equal(7, Evaluate(selector, new Order()));
        Assert.Equal(5, Evaluate(selector, new Order { Other = 1 }));
    }

    // ── Argument scope ──────────────────────────────────────────────────────

    [Fact]
    public void OrderingArguments_AreTypedAgainstTheElement()
    {
        PropertySelector<Order> selector = "Lines|$FN_ORDER_ASC(Weight)";

        Assert.Equal(typeof(int), Arguments(selector.ChildSelector!)[0].Type);
    }

    [Fact]
    public void FunctionsInsideAnOrderingArgument_ResolveAgainstTheElement()
    {
        PropertySelector<Order> selector = "Lines|$FN_ORDER_ASC(Weight|$FN_MATH_MAX(Bonus))|$FN_ELM_FIRST";

        var first = (Line)Evaluate(selector, new Order())!;

        Assert.Equal(2, first.Weight);
    }

    [Fact]
    public void OrderingArguments_ThatOnlyExistOnTheRoot_AreRejectedWhenParsed()
    {
        Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)"Lines|$FN_ORDER_ASC(Quantity)");
    }

    // ── Errors move to parse time in the generic form ───────────────────────

    [Theory]
    [InlineData("$FN_MATH_MAX(Quantity, Missing)")]
    [InlineData("Quantity|$FN_MATH_MAX(Missing)")]
    [InlineData("Maybe|$FN_COALESCE(Missing)")]
    [InlineData("$FN_MATH_MAX(Quantity, $FN_MATH_ROUND(Missing))")]
    public void UnknownPropertyInAnArgument_FailsWhenParsed(string text)
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)text);
        Assert.Contains("'Missing'", exception.Message);

        PropertySelector nonGeneric = text;
        Assert.ThrowsAny<Exception>(() => Body(nonGeneric, out _));
    }

    [Fact]
    public void StructuralErrors_AreReportedBeforeArgumentErrors()
    {
        var count = Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)"$FN_MATH_ROUND(Missing, 1, 2)");
        Assert.StartsWith("$FN_MATH_ROUND can't be called with 3 arguments", count.Message);

        var piped = Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)"Quantity|$FN_STR_JOIN(\",\", Missing)");
        Assert.StartsWith("$FN_STR_JOIN takes no piped value", piped.Message);
    }

    [Fact]
    public void ElementAt_StillReadsItsIntegerLiteral()
    {
        PropertySelector<Order> selector = "Lines|$FN_ELM_ATINDEX(1)";

        Assert.Equal(1, ((Line)Evaluate(selector, new Order())!).Weight);
    }
}
