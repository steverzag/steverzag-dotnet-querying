using Steverzag.PropertySelectors.ArithmeticSelectors;

namespace Steverzag.PropertySelectors.Tests.Registration;

public class PipedFlagTests
{
    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public decimal Price { get; set; }
        public List<int> Numbers { get; set; } = [];
    }

    public static TheoryData<string, bool[]> Chains => new()
    {
        { "$FN_MATH_ROUND(Price)", [false] },
        { "$FN_DT_NOW", [false] },
        { "Name|$FN_STR_TRIM", [true] },
        { "Name|$FN_STR_TRIM|$FN_STR_TOUPPER", [true, true] },
        { "Numbers|$FN_AGGR_MAX", [true] },
        { "Numbers|$FN_AGGR_MAX|$FN_COALESCE(0)", [true, true] },
    };

    [Theory]
    [MemberData(nameof(Chains))]
    public void NonGeneric_FunctionIsPipedUnlessItStartsTheChain(string text, bool[] expected)
    {
        Assert.Equal(expected, PipedFlags((PropertySelector)text));
    }

    [Theory]
    [MemberData(nameof(Chains))]
    public void Generic_FunctionIsPipedUnlessItStartsTheChain(string text, bool[] expected)
    {
        Assert.Equal(expected, PipedFlags((PropertySelector<TestItem>)text));
    }

    [Fact]
    public void AnArgumentIsItsOwnChain()
    {
        foreach (var selector in BothForms("Nickname|$FN_COALESCE(Name|$FN_STR_TRIM, $FN_STR_CONCAT(Name, \"x\"))"))
        {
            var arguments = Parameters(selector.ChildSelector!);

            Assert.Equal([true], PipedFlags(selector));
            Assert.Equal([true], PipedFlags(arguments[0]));
            Assert.Equal([false], PipedFlags(arguments[1]));
        }
    }

    [Fact]
    public void AnArithmeticOperandIsItsOwnChain()
    {
        foreach (var selector in BothForms("Name|$FN_STR_LENGTH + $FN_MATH_ROUND(Price)"))
        {
            var operation = Assert.IsAssignableFrom<IArithmeticOperationSelector>(selector);

            Assert.Equal([true], PipedFlags(operation.LeftExpression));
            Assert.Equal([false], PipedFlags(operation.RightExpression!));
        }
    }

    private static PropertySelector[] BothForms(string text) =>
        [(PropertySelector)text, (PropertySelector<TestItem>)text];

    // Generic chain segments are closed over the piped type, not TestItem, so read Parameters by reflection.
    private static IReadOnlyList<PropertySelector> Parameters(PropertySelector function) =>
        (IReadOnlyList<PropertySelector>)function.GetType().GetProperty(nameof(FunctionPropertySelector.Parameters))!.GetValue(function)!;

    private static bool[] PipedFlags(PropertySelector selector)
    {
        var flags = new List<bool>();

        for (var segment = selector; segment is not null; segment = segment.ChildSelector)
        {
            if (segment is IPipeableFunctionSelector function)
                flags.Add(function.IsPiped);
        }

        return [.. flags];
    }
}
