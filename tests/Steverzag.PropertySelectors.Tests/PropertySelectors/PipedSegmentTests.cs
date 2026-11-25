using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class PipedSegmentTests
{
    public class Order
    {
        public string Name { get; set; } = "  Ann ";
        public string? Nickname { get; set; }
        public int Quantity { get; set; } = 4;
        public DateTime Created { get; set; } = new(2024, 1, 10, 8, 0, 0);
        public DateTime? Deleted { get; set; }
        public Address Address { get; set; } = new();
        public List<Phone> Phones { get; set; } =
        [
            new() { Id = 1, Number = "111", Tags = ["home", "main"] },
            new() { Id = 2, Number = "2222", Tags = ["work"] },
        ];
    }

    public class Address
    {
        public string City { get; set; } = "Lima";
    }

    public class Phone
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public List<string> Tags { get; set; } = [];
    }

    private static Expression Body(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(Order), "o");
        return ExpressionUtils.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate(PropertySelector selector, Order order)
    {
        var body = Body(selector, out var parameter);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(order);
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Order>)text : (PropertySelector)text;

    public static TheoryData<string, object?> Values => new()
    {
        { "Address|City", "Lima" },
        { "Address|City|$FN_STR_LENGTH", 4 },
        { "Created|Year", 2024 },
        { "Created|$FN_DT_ADDDAYS(30)|Month", 2 },
        { "Created|$FN_DT_ADDDAYS(30)|Date.Day", 9 },
        { "Created|$FN_DT_ADDDAYS(30)|Month|$FN_MATH_MAX(Quantity)", 4 },
        { "Deleted|Year", null },
        { "Deleted|$FN_COALESCE(Created)|Year", 2024 },
        { "Name|$FN_STR_TRIM|Length", 3 },
        { "Nickname|$FN_COALESCE(Name)|Length", 6 },
        { "Phones|$FN_ELM_FIRST|Number", "111" },
        { "Phones|$FN_ELM_LAST|Number|$FN_STR_LENGTH", 4 },
        { "Phones|$FN_ELM_FIRST|Tags|$FN_AGGR_COUNT", 2 },
        { "Phones|$FN_ORDER_DESC(Id)|$FN_ELM_FIRST|Number", "2222" },
        { "Phones|Id|$FN_AGGR_SUM", 3 },
        { "Phones|Number|$FN_AGGR_COUNT", 2 },
        { "Created|$FN_DT_ADDDAYS(30)|Month * 2", 4 },
        { "$FN_MATH_MAX(Created|$FN_DT_ADDDAYS(30)|Month, 1)", 2 },
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void GenericForm_ResolvesTheSegmentAgainstThePipedValue(string text, object? expected)
    {
        PropertySelector<Order> selector = text;

        Assert.Equal(Body(selector, out _).Type, selector.Type);
        Assert.Equal(expected, Evaluate(selector, new Order()));
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void NonGenericForm_ResolvesTheSegmentAgainstThePipedValue(string text, object? expected)
    {
        PropertySelector selector = text;

        Assert.Equal(expected, Evaluate(selector, new Order()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALiftedPart_ReadsTheValueWhenThereIsOne(bool generic)
    {
        var selector = Parse("Deleted|Year", generic);

        Assert.Equal(typeof(int?), Body(selector, out _).Type);
        Assert.Equal(2023, Evaluate(selector, new Order { Deleted = new(2023, 5, 1) }));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANowFunction_IsFollowedByAMember(bool generic)
    {
        var selector = Parse("$FN_DT_NOW|Year", generic);

        Assert.Equal(DateTime.Now.Year, Evaluate(selector, new Order()));
    }

    [Theory]
    [InlineData("Address|City", "Address.City", true)]
    [InlineData("Address|City", "Address.City", false)]
    [InlineData("Phones|Number", "Phones.Number", true)]
    [InlineData("Phones|Number", "Phones.Number", false)]
    [InlineData("Phones|$FN_ELM_FIRST|Tags", "Phones|$FN_ELM_FIRST|Tags", true)]
    public void APipedProperty_BuildsWhatTheDottedPathBuilds(string piped, string dotted, bool generic)
    {
        var pipedSelector = Parse(piped, generic);
        var dottedSelector = Parse(dotted, generic);

        Assert.Equal(Body(dottedSelector, out _).ToString(), Body(pipedSelector, out _).ToString());
        Assert.Equal(dottedSelector.Type, pipedSelector.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACollectionPipedIntoAProperty_Projects(bool generic)
    {
        var selector = Parse("Phones|Number", generic);

        Assert.Equal(new[] { "111", "2222" }, (IEnumerable<string>)Evaluate(selector, new Order())!);
    }

    [Fact]
    public void GenericForm_DeclaresTheMemberType()
    {
        PropertySelector<Order> selector = "Created|$FN_DT_ADDDAYS(3)|Year";

        Assert.Equal(typeof(int), selector.Type);
    }

    [Theory]
    [InlineData("Created|$FN_DT_ADDDAYS(3)|Year")]
    [InlineData("Address|City")]
    [InlineData("Phones|$FN_ELM_FIRST|Number")]
    public void ToString_WritesThePipedForm(string text)
    {
        Assert.Equal(text, ((PropertySelector<Order>)text).ToString());
        Assert.Equal(text, ((PropertySelector)text).ToString());
    }

    // ── Rejections ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUnknownMember_IsLookedUpOnThePipedType(bool generic)
    {
        var e = Assert.Throws<SelectorParseException>(() =>
        {
            var selector = Parse("Created|$FN_DT_ADDDAYS(3)|Missing", generic);
            Body(selector, out _);
        });

        Assert.Equal("Property 'Missing' was not found on type 'DateTime'.", e.Detail);
        if (generic)
            Assert.Equal(26, e.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADotAfterTheArguments_IsRejectedWithThePipedForm(bool generic)
    {
        var e = Assert.Throws<SelectorParseException>(() => Parse("Created|$FN_DT_ADDDAYS(3).Year", generic));

        Assert.Equal("A member can't follow a function with '.'. Write it after a pipe: '$FN_DT_ADDDAYS(3)|Year'.", e.Detail);
        Assert.Equal(25, e.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADotAfterAFunctionName_IsRejectedWithThePipedForm(bool generic)
    {
        var e = Assert.Throws<SelectorParseException>(() => Parse("Phones|$FN_ELM_FIRST.Number", generic));

        Assert.Equal("A member can't follow a function with '.'. Write it after a pipe: '$FN_ELM_FIRST|Number'.", e.Detail);
        Assert.Equal(20, e.Position);
    }
}
