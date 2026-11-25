using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ArithmeticSelectors;

/// <summary>
/// Covers the Math function selectors end to end: the string is parsed into a selector chain,
/// the chain is turned into an expression and the expression is executed.
/// </summary>
public class MathSelectorTests
{
    private class TestItem
    {
        public int Amount { get; set; }
        public double Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Math functions resolve their parameters against the chained target, so the whole
    // selector chain has to be walked (a single PropertyAccess call only covers one part).
    private static TResult Evaluate<TResult>(PropertySelector<TestItem> selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }


    // ── Powers and roots ────────────────────────────────────────────────────

    [Fact]
    public void MathPow_WithTwoLiterals_RaisesToThePower()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathPower}(2, 3)";

        var result = Evaluate<double>(selector, new TestItem());

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(8d, result);
    }

    [Fact]
    public void MathPow_WithPropertyParameters_RaisesToThePower()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathPower}(Value, Amount)";

        var result = Evaluate<double>(selector, new TestItem { Value = 3, Amount = 2 });

        Assert.Equal(9d, result);
    }

    [Fact]
    public void MathSqrt_WithLiteral_ReturnsSquareRoot()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathSqrt}(9)";

        var result = Evaluate<double>(selector, new TestItem());

        Assert.Equal(3d, result);
    }

    [Fact]
    public void MathSqrt_WithPropertyParameter_ReturnsSquareRoot()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathSqrt}(Value)";

        var result = Evaluate<double>(selector, new TestItem { Value = 16 });

        Assert.Equal(4d, result);
    }

    [Fact]
    public void MathSqrt_OnNonGenericSelector_ReturnsSquareRoot()
    {
        PropertySelector selector = $"{SelectorDefaults.MathSqrt}(Value)";

        var result = Evaluate<double>(selector, new TestItem { Value = 25 });

        Assert.Equal(5d, result);
    }

    [Fact]
    public void MathExp_WithZero_ReturnsOne()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathExp}(0)";

        Assert.Equal(1d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathLog_WithOne_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathLog}(1)";

        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathLog10_WithHundred_ReturnsTwo()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathLog10}(100)";

        Assert.Equal(2d, Evaluate<double>(selector, new TestItem()));
    }

    // ── Rounding ────────────────────────────────────────────────────────────

    [Fact]
    public void MathCeiling_WithFractionalValue_RoundsUp()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathCeiling}(1.2)";

        Assert.Equal(2d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathFloor_WithFractionalValue_RoundsDown()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathFloor}(1.8)";

        Assert.Equal(1d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathTruncate_WithFractionalValue_DropsTheFraction()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathTruncate}(-1.9)";

        Assert.Equal(-1d, Evaluate<double>(selector, new TestItem()));
    }

    [Theory]
    [InlineData("1.5", 2d)]
    [InlineData("2.5", 2d)]
    [InlineData("2.4", 2d)]
    public void MathRound_WithSingleParameter_UsesBankersRounding(string payload, double expected)
    {
        // Math.Round(double) rounds midpoints to the nearest even number.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathRound}({payload})";

        Assert.Equal(expected, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathRound_WithDigitsParameter_RoundsToThatManyDigits()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathRound}(1.2345, 2)";

        Assert.Equal(1.23d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathCeiling_WithIntegerLiteral_ReportsAndEvaluatesToDouble()
    {
        // Math has no Ceiling(int), so it falls back to Ceiling(double) and declares double.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathCeiling}(3)";

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(3d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathCeiling_WithDecimalLiteral_ReportsAndEvaluatesToDouble()
    {
        // Decimals stay on the double overload so EF's SQLite provider can translate them.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathCeiling}(2.1m)";

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(3d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathCeiling_WithPropertyParameter_ReportsNoTypeButStillEvaluates()
    {
        // Parameters are always parsed as non-generic selectors, which carry no SelectorType
        // for a plain property path, so the parameter-derived SelectorType degrades to null.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathCeiling}(Value)";

        Assert.Null(selector.Type);
        Assert.Equal(2d, Evaluate<double>(selector, new TestItem { Value = 1.2 }));
    }

    // ── Sign ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("-4.5", -1)]
    [InlineData("0.0", 0)]
    [InlineData("4.5", 1)]
    public void MathSign_ReturnsTheSignAsInteger(string payload, int expected)
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathSign}({payload})";

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(expected, Evaluate<int>(selector, new TestItem()));
    }

    // ── Trigonometry ────────────────────────────────────────────────────────

    [Fact]
    public void MathSin_WithZero_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathSin}(0)";

        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathCos_WithZero_ReturnsOne()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathCos}(0)";

        Assert.Equal(1d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathTan_WithZero_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathTan}(0)";

        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathAsin_WithZero_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathAsin}(0)";

        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathAcos_WithOne_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathAcos}(1)";

        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathAtan_WithZero_ReturnsZero()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathAtan}(0)";

        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathAtan2_WithTwoParameters_ReturnsTheAngle()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathAtan2}(0, 1)";

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(0d, Evaluate<double>(selector, new TestItem()));
    }

    // ── Max / Min ───────────────────────────────────────────────────────────

    [Fact]
    public void MathMax_WithTwoLiterals_ReturnsTheLarger()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathMax}(2.0, 5.0)";

        Assert.Equal(5d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathMin_WithTwoLiterals_ReturnsTheSmaller()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathMin}(2.0, 5.0)";

        Assert.Equal(2d, Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathMax_SetsSelectorTypeToTheWiderParameterType()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathMax}(Amount, 5.0)";

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(5d, Evaluate<double>(selector, new TestItem { Amount = 2 }));
    }

    [Fact]
    public void MathMax_WithIntegerParameters_ReportsAndEvaluatesToInt()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathMax}(2, 5)";

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(5, Evaluate<int>(selector, new TestItem()));
    }

    [Fact]
    public void MathMax_WithMixedIntegerSizes_UsesTheWiderOverload()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathMax}(2, 5L)";

        Assert.Equal(typeof(long), selector.Type);
        Assert.Equal(5L, Evaluate<long>(selector, new TestItem()));
    }

    // ── Nesting and failures ────────────────────────────────────────────────

    [Fact]
    public void MathFunctions_AcceptNestedFunctionParameters()
    {
        // The parameter parser tracks nested parentheses, so a function can be an argument.
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.MathRound}({SelectorDefaults.MathSqrt}(Value), 3)";

        var result = Evaluate<double>(selector, new TestItem { Value = 2 });

        Assert.Equal(Math.Round(Math.Sqrt(2), 3), result);
    }

    [Fact]
    public void MathSqrt_WithNonNumericParameter_ThrowsInvalidOperationExceptionOnBuild()
    {
        // The numeric check happens lazily when the expression is built, not at parse time.
        PropertySelector<TestItem> selector = $"{SelectorDefaults.MathSqrt}(Name)";

        Assert.Throws<InvalidOperationException>(() => Evaluate<double>(selector, new TestItem()));
    }

    [Fact]
    public void MathSqrt_WithoutParameters_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            PropertySelector<TestItem> selector = SelectorDefaults.MathSqrt;
        });
    }

    [Fact]
    public void MathPow_WithASingleParameter_ThrowsArgumentException()
    {
        // Only the two-parameter overload is registered for Pow.
        Assert.Throws<ArgumentException>(() =>
        {
            PropertySelector<TestItem> selector = $"{SelectorDefaults.MathPower}(2)";
        });
    }
}
