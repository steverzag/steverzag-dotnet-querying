using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class SelectorParseExceptionTests
{
    public class Order
    {
        public int Amount { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public List<Line> Lines { get; set; } = [];
    }

    public class Line
    {
        public double Weight { get; set; }
    }

    private static SelectorParseException Generic(string text)
        => Assert.Throws<SelectorParseException>(() => (PropertySelector<Order>)text);

    private static SelectorParseException NonGeneric(string text)
        => Assert.Throws<SelectorParseException>(() => (PropertySelector)text);

    private static Expression Body(PropertySelector selector)
        => ExpressionUtils.CreateMemberAccess(Expression.Parameter(typeof(Order), "x"), selector);

    [Fact]
    public void IsAnArgumentException()
    {
        Assert.IsAssignableFrom<ArgumentException>(Generic("Missing"));
    }

    [Fact]
    public void UnknownPropertyInAChain_GivesItsPositionAndToken()
    {
        var exception = Generic("Lines|Missing");

        Assert.Equal("Property 'Missing' was not found on type 'Order'.", exception.Detail);
        Assert.Equal("Lines|Missing", exception.Selector);
        Assert.Equal(6, exception.Position);
        Assert.Equal("Missing", exception.Token);
        Assert.Equal("Property 'Missing' was not found on type 'Order'. Position 6 in 'Lines|Missing'.", exception.Message);
    }

    [Fact]
    public void UnknownPropertyInANestedPath_PointsAtTheMissingSegment()
    {
        var exception = Generic("Lines.Missing");

        Assert.Equal("Property 'Missing' was not found on type 'Line'.", exception.Detail);
        Assert.Equal(6, exception.Position);
        Assert.Equal("Missing", exception.Token);
    }

    [Fact]
    public void UnknownProperty_IsReportedTheSameWayInBothForms()
    {
        var generic = Generic("Lines.Missing");
        var nonGeneric = Assert.Throws<SelectorParseException>(() => Body((PropertySelector)"Lines.Missing"));

        Assert.Equal(generic.Detail, nonGeneric.Detail);
        Assert.Equal(generic.Token, nonGeneric.Token);
        Assert.Equal(generic.Position, nonGeneric.Position);
        Assert.Equal(generic.Message, nonGeneric.Message);
    }

    [Fact]
    public void UnknownPropertyInAnArgument_PointsInsideTheArgument()
    {
        const string text = "$FN_MATH_MAX(Amount, $FN_MATH_SQRT(Missing))";

        var exception = Generic(text);

        Assert.Equal(text.IndexOf("Missing"), exception.Position);
        Assert.Equal("Missing", exception.Token);
        Assert.Equal(text, exception.Selector);
    }

    [Fact]
    public void UnknownFunction_IsNamed()
    {
        foreach (var exception in new[] { Generic("Name|$FN_NOPE(1)"), NonGeneric("Name|$FN_NOPE(1)") })
        {
            Assert.Equal("Unknown function '$FN_NOPE'. Position 5 in 'Name|$FN_NOPE(1)'.", exception.Message);
            Assert.Equal("$FN_NOPE", exception.Token);
        }
    }

    [Fact]
    public void ArgumentTypeErrors_GiveTheFunctionPosition()
    {
        var exception = Generic("Name|$FN_MATH_SQRT");

        Assert.Equal(5, exception.Position);
        Assert.StartsWith("$FN_MATH_SQRT: the piped value 'Name' must be a number", exception.Detail);
    }

    [Theory]
    [InlineData("Amount * Missing", 9, "Missing")]
    [InlineData("Amount * 2 3", 11, "3")]
    [InlineData("Amount * #2", 9, "#")]
    [InlineData("(Amount + 2", 0, "(")]
    [InlineData("Amount +", 8, null)]
    public void ArithmeticErrors_GiveTheirPosition(string text, int position, string? token)
    {
        var exception = Generic(text);

        Assert.Equal(text, exception.Selector);
        Assert.Equal(position, exception.Position);
        Assert.Equal(token, exception.Token);
    }

    [Fact]
    public void LeftoverOperand_IsRejected()
    {
        // The parser used to stop after "Amount * 2" and silently drop the rest.
        var exception = NonGeneric("Amount * 2 3");

        Assert.Equal("Unexpected '3': an operator is missing before it. Position 11 in 'Amount * 2 3'.", exception.Message);
    }

    [Theory]
    [InlineData("Name)", "Unbalanced parentheses: unexpected ')'.", 4)]
    [InlineData("$FN_COALESCE(Nickname, \"x)", "Unterminated string literal: a '\"' is never closed.", 23)]
    [InlineData("Name||Amount", "Empty part: '|' must sit between two parts.", 5)]
    [InlineData("Name|", "Empty part: '|' must sit between two parts.", 5)]
    [InlineData("$FN_MATH_MAX(Amount,,1)", "Empty argument 2.", 20)]
    [InlineData("$FN_MATH_SQRT(Amount)x", "Unexpected 'x' after the closing ')'.", 21)]
    public void SyntaxErrors_AreRejectedInBothForms(string text, string detail, int position)
    {
        foreach (var exception in new[] { Generic(text), NonGeneric(text) })
        {
            Assert.Equal(detail, exception.Detail);
            Assert.Equal(position, exception.Position);
        }
    }

    [Fact]
    public void WithoutAPosition_TheMessageIsTheDetail()
    {
        var exception = new SelectorParseException("Something is wrong.", "Name");

        Assert.Equal("Something is wrong.", exception.Message);
    }

    [Fact]
    public void TheTokenIsAddedWhenTheDetailDoesNotNameIt()
    {
        var exception = new SelectorParseException("Something is wrong.", "Name|Other", 5, "Other");

        Assert.Equal("Something is wrong. Position 5 ('Other') in 'Name|Other'.", exception.Message);
    }
}
