using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ArithmeticSelectors;

public class LiteralPipeTests
{
    public class Order
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public decimal Price { get; set; } = 2.5m;
    }

    private static Expression Body(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(Order), "o");
        return ExpressionUtils.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate(PropertySelector selector)
    {
        var body = Body(selector, out var parameter);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Order());
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Order>)text : (PropertySelector)text;

    public static TheoryData<string, object?, bool> Values
    {
        get
        {
            var data = new TheoryData<string, object?, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("Age + 4|$FN_MATH_SQRT", 32.0, generic);
                data.Add("Age + 2.25|$FN_MATH_SQRT", 31.5, generic);
                data.Add("4|$FN_MATH_SQRT + Age", 32.0, generic);
                data.Add("Age * 9|$FN_MATH_SQRT|$FN_MATH_ROUND", 90.0, generic);
                data.Add("Age + 2|$FN_MATH_POW(3)", 38.0, generic);
                data.Add("Age + 5L|$FN_MATH_MAX(7L)", 37L, generic);
                data.Add("Price * 3m|$FN_MATH_ROUND(0)", 7.5m, generic);
                data.Add("Age + \"ab\"|$FN_STR_LENGTH", 32, generic);
                data.Add("\"ab\"|$FN_STR_LENGTH * Age", 60, generic);
                data.Add("Age + \"a|b\"|$FN_STR_LENGTH", 33, generic);
                data.Add("Age + \"say \"\"hi\"\"\"|$FN_STR_LENGTH", 38, generic);
                data.Add("Age + \"  ab \"|$FN_STR_TRIM|$FN_STR_LENGTH", 32, generic);
                data.Add("Age - -4|$FN_MATH_SQRT", 32.0, generic);
                data.Add("-4|$FN_MATH_SQRT", -2.0, generic);
                data.Add("-4|$FN_MATH_SQRT + Age", 28.0, generic);
                data.Add("Age + 4 |$FN_MATH_SQRT", 32.0, generic);
                data.Add("Age + 4| $FN_MATH_SQRT", 32.0, generic);
                data.Add("Age + 4 | $FN_MATH_SQRT | $FN_MATH_ROUND", 32.0, generic);
                data.Add("Age + Age |$FN_MATH_SQRT", 30 + Math.Sqrt(30), generic);
                data.Add("Age + Name| $FN_STR_SUBSTRING(0, 2)|$FN_STR_LENGTH", 32, generic);
                data.Add("$FN_MATH_MAX(1 + 4|$FN_MATH_SQRT, 2)", 3.0, generic);
                data.Add("(Age + 9|$FN_MATH_SQRT)|$FN_MATH_ROUND", 33.0, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void ALiteralInsideArithmetic_CanBePiped(string text, object? expected, bool generic)
    {
        var selector = Parse(text, generic);

        Assert.Equal(expected, Evaluate(selector));
        if (generic || selector.Type is not null)
            Assert.Equal(Body(selector, out _).Type, selector.Type);
    }

    [Theory]
    [InlineData("Age + 4|$FN_MATH_SQRT", "Age + 4|$FN_MATH_SQRT")]
    [InlineData("Age + 4 | $FN_MATH_SQRT", "Age + 4|$FN_MATH_SQRT")]
    [InlineData("Age + \"ab\"|$FN_STR_LENGTH", "Age + \"ab\"|$FN_STR_LENGTH")]
    [InlineData("-4|$FN_MATH_SQRT", "-4|$FN_MATH_SQRT")]
    public void ToString_KeepsThePipeOnTheLiteral_AndReparses(string text, string expected)
    {
        foreach (var generic in new[] { true, false })
        {
            var selector = Parse(text, generic);

            Assert.Equal(expected, selector.ToString());
            Assert.Equal(Evaluate(selector), Evaluate(Parse(selector.ToString(), generic)));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnaryMinus_AppliesAfterThePipe(bool generic)
    {
        var piped = Body(Parse("-4|$FN_MATH_SQRT", generic), out _);

        Assert.Equal(ExpressionType.Negate, piped.NodeType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlainNegativeLiterals_AreStillLiterals(bool generic)
    {
        var body = Body(Parse("Age * -2", generic), out _);

        Assert.Equal("(o.Age * -2)", body.ToString());
        Assert.Equal("Age * -2", Parse("Age * -2", generic).ToString());
    }

    [Theory]
    [InlineData("Age + 4|", 8)]
    [InlineData("Age + 4 | ", 9)]
    [InlineData("Age + \"ab\"|", 11)]
    public void APipeWithNothingAfterIt_IsAnEmptyPart(string text, int position)
    {
        foreach (var generic in new[] { true, false })
        {
            var exception = Assert.Throws<SelectorParseException>(() => Parse(text, generic));

            Assert.Equal("Empty part: '|' must sit between two parts.", exception.Detail);
            Assert.Equal(position, exception.Position);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUnknownMemberAfterAPipedLiteral_GivesItsPosition(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Body(Parse("Age + 4|Missing", generic), out _));

        Assert.Equal("Property 'Missing' was not found on type 'Int32'.", exception.Detail);
        if (generic)
            Assert.Equal(8, exception.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMissingOperator_IsStillReported(bool generic)
    {
        var exception = Assert.Throws<SelectorParseException>(() => Parse("Age + 4 3|$FN_MATH_SQRT", generic));

        Assert.StartsWith("Unexpected '3|$FN_MATH_SQRT': an operator is missing before it.", exception.Message);
    }
}
