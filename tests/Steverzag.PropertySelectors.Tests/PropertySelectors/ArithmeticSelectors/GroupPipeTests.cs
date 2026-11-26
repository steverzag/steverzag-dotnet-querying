using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ArithmeticSelectors;

public class GroupPipeTests
{
    public class Order
    {
        public int Quantity { get; set; } = 4;
        public int Age { get; set; } = 30;
        public int? Bonus { get; set; }
        public decimal Price { get; set; } = 1.111m;
        public string Name { get; set; } = "Ann";
        public string? Nickname { get; set; }
        public DateTime Created { get; set; } = new(2024, 1, 10);
        public List<int> Phones { get; set; } = [1, 2];
    }

    private static Expression Body(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(Order), "o");
        return ExpressionUtils.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate(PropertySelector selector, Order? order = null)
    {
        var body = Body(selector, out var parameter);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(order ?? new Order());
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Order>)text : (PropertySelector)text;

    public static TheoryData<string, object?> Values => new()
    {
        { "(Quantity + 1)|$FN_MATH_MAX(10)", 10 },
        { "(Price * 3)|$FN_MATH_ROUND(2)", 3.33m },
        { "(Quantity + 1)|$FN_MATH_MAX(Age)", 30 },
        { "(Quantity + 1)|$FN_MATH_MIN(3) + 10", 13 },
        { "((Quantity + 1)|$FN_MATH_MAX(10)) * 2", 20 },
        { "Quantity + (Quantity * 2)|$FN_MATH_MIN(5)", 9 },
        { "-(Quantity + 1)|$FN_MATH_MAX(6)", -6 },
        { "(Bonus + 1)|$FN_COALESCE(0)", 0 },
        { "(Phones|$FN_AGGR_COUNT + 1)|$FN_MATH_MAX(5)", 5 },
        { "(Created)|Year", 2024 },
        { "(Created)|$FN_DT_ADDDAYS(30)|Month", 2 },
        { "(Nickname)|$FN_COALESCE(Name)|Length", 3 },
        { "(Quantity + 1) | $FN_MATH_MAX(10)", 10 },
        { "$FN_MATH_MAX((Quantity + 1)|$FN_MATH_MIN(3), 2)", 3 },
        { "($FN_MATH_MAX(Quantity, 1)) * 2", 8 },
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void GenericForm_PipesTheGroup(string text, object? expected)
    {
        PropertySelector<Order> selector = text;

        Assert.Equal(Body(selector, out _).Type, selector.Type);
        Assert.Equal(expected, Evaluate(selector));
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void NonGenericForm_PipesTheGroup(string text, object? expected)
    {
        PropertySelector selector = text;

        Assert.Equal(expected, Evaluate(selector));
    }

    [Theory]
    [InlineData("(Quantity + 1)|$FN_MATH_MAX(10)", "$FN_MATH_MAX(Quantity + 1, 10)", true)]
    [InlineData("(Quantity + 1)|$FN_MATH_MAX(10)", "$FN_MATH_MAX(Quantity + 1, 10)", false)]
    [InlineData("(Price * 3)|$FN_MATH_ROUND(2)", "$FN_MATH_ROUND(Price * 3, 2)", true)]
    [InlineData("(Price * 3)|$FN_MATH_ROUND(2)", "$FN_MATH_ROUND(Price * 3, 2)", false)]
    public void APipedGroup_BuildsWhatTheStaticFormBuilds(string piped, string staticForm, bool generic)
    {
        var pipedBody = Body(Parse(piped, generic), out var pipedParameter);
        var staticBody = Body(Parse(staticForm, generic), out var staticParameter);

        Assert.Equal(staticBody.ToString().Replace(staticParameter.Name!, "_"), pipedBody.ToString().Replace(pipedParameter.Name!, "_"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheGroupDeclaresTheTypeOfItsLastPart(bool generic)
    {
        var group = Parse("(Bonus + 1)", generic);
        var piped = Parse("(Bonus + 1)|$FN_COALESCE(0)", generic);

        Assert.Equal(typeof(int?), Body(group, out _).Type);
        Assert.Equal(typeof(int), Body(piped, out _).Type);
        if (generic)
        {
            Assert.Equal(typeof(int?), group.Type);
            Assert.Equal(typeof(int), piped.Type);
        }
    }

    [Theory]
    [InlineData("(Quantity + 1)|$FN_MATH_MAX(10)", "(Quantity + 1)|$FN_MATH_MAX(10)")]
    [InlineData("((Quantity + 1)|$FN_MATH_MAX(10)) * 2", "(Quantity + 1)|$FN_MATH_MAX(10) * 2")]
    [InlineData("-(Quantity + 1)|$FN_MATH_MAX(6)", "-(Quantity + 1)|$FN_MATH_MAX(6)")]
    [InlineData("(Quantity + 1) | $FN_MATH_MAX(10)", "(Quantity + 1)|$FN_MATH_MAX(10)")]
    [InlineData("(Created)|Year", "Created|Year")]
    public void ToString_KeepsTheGroupInFrontOfThePipe(string text, string expected)
    {
        foreach (var generic in new[] { true, false })
            Assert.Equal(expected, Parse(text, generic).ToString());
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void ToString_RoundTrips(string text, object? expected)
    {
        foreach (var generic in new[] { true, false })
        {
            var printed = Parse(text, generic).ToString();
            var reparsed = Parse(printed, generic);

            Assert.Equal(printed, reparsed.ToString());
            Assert.Equal(expected, Evaluate(reparsed));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AGroupAfterAPipe_IsRejected(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse("Quantity|(Quantity + 1)", generic));

        Assert.Equal("A group can only start a selector, not follow '|'.", exception.Detail);
        Assert.Equal(9, exception.Position);
        Assert.Equal("(Quantity + 1)", exception.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APipeWithNothingAfterTheGroup_IsRejected(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse("(Quantity + 1)|", generic));

        Assert.StartsWith("Empty part", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APipeWithNothingAfterTheGroup_InsideArithmetic_IsRejected(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse("(Quantity + 1)| * 2", generic));

        Assert.Equal("Empty part: '|' must sit between two parts. Position 14 in '(Quantity + 1)| * 2'.", exception.Message);
    }

    [Fact]
    public void AnUnknownMemberAfterTheGroup_GivesItsPosition()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)"(Quantity + 1)|Missing * 2");

        Assert.Equal("Missing", exception.Token);
        Assert.Equal(15, exception.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANonNumericGroup_StillFailsAsArithmetic(bool generic)
    {
        var exception = Record.Exception(() => Evaluate(Parse("(Name + 1)|$FN_MATH_MAX(10)", generic)));

        Assert.NotNull(exception);
        Assert.IsNotType<NullReferenceException>(exception);
    }

    [Fact]
    public void APipeAfterAPlainOperand_InsideArithmetic_IsStillAnUnexpectedCharacter()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)"1 + Quantity |$FN_MATH_MAX(10)");

        Assert.StartsWith("Unexpected character '|'.", exception.Message);
    }
}
