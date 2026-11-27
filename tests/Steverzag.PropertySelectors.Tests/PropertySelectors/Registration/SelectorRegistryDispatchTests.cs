using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.OrderingSelectors;
using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.Registration;

public class SelectorRegistryDispatchTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Registry-resolved selectors apply straight to the source they are handed, so the source
    /// itself is the target expression here (no property path in front of it).
    /// </summary>
    private static TResult Evaluate<TSource, TResult>(PropertySelector selector, TSource source)
    {
        var parameter = Expression.Parameter(typeof(TSource), "x");
        var body = SelectorExpressions.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TSource, TResult>>(body, parameter).Compile();
        return lambda(source);
    }

    private static List<TestItem> SampleItems() =>
    [
        new TestItem { Id = 3, Name = "c" },
        new TestItem { Id = 1, Name = "a" },
        new TestItem { Id = 2, Name = "b" },
    ];

    [Fact]
    public void ImplicitOperator_OrderAscendingBareToken_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.OrderAscending;

        // Assert
        Assert.IsType<OrderByAscendingSelector>(selector);
    }

    [Fact]
    public void ImplicitOperator_OrderAscendingEmptyParentheses_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.OrderAscending + "()";

        // Assert
        Assert.IsType<OrderByAscendingSelector>(selector);
    }

    [Fact]
    public void ImplicitOperator_OrderAscendingWithParameter_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.OrderAscending + "(Id)";

        // Assert
        var orderSelector = Assert.IsType<OrderByAscendingSelector>(selector);
        Assert.Single(orderSelector.Parameters);
    }

    [Fact]
    public void ImplicitOperator_OrderDescendingBareToken_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.OrderDescending;

        // Assert
        Assert.IsType<OrderByDescendingSelector>(selector);
    }

    [Fact]
    public void ImplicitOperator_OrderDescendingEmptyParentheses_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.OrderDescending + "()";

        // Assert
        Assert.IsType<OrderByDescendingSelector>(selector);
    }

    [Fact]
    public void ImplicitOperator_OrderDescendingWithParameter_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.OrderDescending + "(Id)";

        // Assert
        var orderSelector = Assert.IsType<OrderByDescendingSelector>(selector);
        Assert.Single(orderSelector.Parameters);
    }

    // MathPowSelector and MathSqrtSelector are internal, so they are asserted through PlainValue and SelectorType.

    [Fact]
    public void ImplicitOperator_MathPowWithParameters_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.MathPower + "(2, 3)";

        // Assert
        Assert.StartsWith(SelectorDefaults.MathPower, selector.PlainValue);
        Assert.Equal(typeof(double), selector.SelectorType);
    }

    [Fact]
    public void ImplicitOperator_MathSqrtWithParameter_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.MathSqrt + "(4)";

        // Assert
        Assert.StartsWith(SelectorDefaults.MathSqrt, selector.PlainValue);
        Assert.Equal(typeof(double), selector.SelectorType);
    }

    [Fact]
    public void GenericImplicitOperator_OrderAscendingWithParameter_ResolvesFromRegistry()
    {
        // Act
        PropertySelector<List<TestItem>> selector = SelectorDefaults.OrderAscending + "(Id)";

        // Assert
        var orderSelector = Assert.IsType<OrderByAscendingSelector<List<TestItem>>>(selector);
        Assert.Single(orderSelector.Parameters);
    }

    [Fact]
    public void GenericImplicitOperator_OrderAscendingEmptyParentheses_ResolvesFromRegistry()
    {
        // Previously the generic operator only matched the bare token and threw on "TOKEN()";
        // registry resolution accepts both forms in both operators.

        // Act
        PropertySelector<List<TestItem>> selector = SelectorDefaults.OrderAscending + "()";

        // Assert
        Assert.IsType<OrderByAscendingSelector<List<TestItem>>>(selector);
    }

    [Fact]
    public void ImplicitOperator_UnknownFunctionToken_ThrowsSelectorParseException()
    {
        // Act & Assert
        Assert.Throws<SelectorParseException>(() => (PropertySelector)"$FN_NOPE(1)");
    }

    [Fact]
    public void ImplicitOperator_AggregationToken_ResolvesFromRegistry()
    {
        // Act
        PropertySelector selector = SelectorDefaults.CountAggregateFunction;

        // Assert
        Assert.StartsWith(SelectorDefaults.CountAggregateFunction, selector.PlainValue);
    }

    [Fact]
    public void ImplicitOperator_FunctionWithUnsupportedParameterCount_ThrowsSelectorParseException()
    {
        // Aggregation selectors only define a parameterless constructor.

        // Act & Assert
        Assert.Throws<SelectorParseException>(() => (PropertySelector)(SelectorDefaults.SumAggregateFunction + "(Id)"));
    }

    [Fact]
    public void Initialize_IsIdempotent_AndResolutionWorksAfterwards()
    {
        // Act
        SelectorRegistry.Initialize();
        SelectorRegistry.Initialize();
        PropertySelector selector = SelectorDefaults.OrderAscending;

        // Assert
        Assert.IsType<OrderByAscendingSelector>(selector);
    }

    [Fact]
    public void ImplicitOperator_RepeatedConversions_ProduceIndependentInstances()
    {
        // Two conversions of the same string must not share state (factories are cached, instances are not).

        // Act
        PropertySelector first = SelectorDefaults.OrderAscending + "(Id)";
        PropertySelector second = SelectorDefaults.OrderAscending + "(Id)";

        // Assert
        Assert.NotSame(first, second);
    }

    [Fact]
    public void GenericImplicitOperator_TopLevelArithmeticExpression_CreatesArithmeticOperationSelector()
    {
        // Act
        PropertySelector<TestItem> selector = "Id * 2";

        // Assert
        Assert.IsType<ArithmeticOperationSelector<TestItem>>(selector);
    }

    [Fact]
    public void ImplicitOperator_OperatorInsideParentheses_IsNotTreatedAsTopLevelArithmetic()
    {
        // Act
        PropertySelector selector = SelectorDefaults.MathPower + "(2+1, 3)";

        // Assert
        Assert.StartsWith(SelectorDefaults.MathPower, selector.PlainValue);
        Assert.Equal(typeof(double), selector.SelectorType);
    }

    // ── Evaluation of the resolved selectors ────────────────────────────────
    // Resolution is only half the contract: the instance the registry builds also has to
    // produce a working expression.

    [Fact]
    public void Evaluate_MathPowResolvedFromRegistry_ComputesThePower()
    {
        PropertySelector selector = SelectorDefaults.MathPower + "(2, 3)";

        var result = Evaluate<TestItem, double>(selector, new TestItem());

        Assert.Equal(8d, result);
    }

    [Fact]
    public void Evaluate_MathSqrtResolvedFromRegistry_ComputesTheSquareRoot()
    {
        PropertySelector selector = SelectorDefaults.MathSqrt + "(4)";

        var result = Evaluate<TestItem, double>(selector, new TestItem());

        Assert.Equal(2d, result);
    }

    [Fact]
    public void Evaluate_MathPowWithInnerArithmetic_ComputesTheArgumentFirst()
    {
        PropertySelector selector = SelectorDefaults.MathPower + "(2+1, 3)";

        var result = Evaluate<TestItem, double>(selector, new TestItem());

        Assert.Equal(27d, result);
    }

    [Fact]
    public void Evaluate_OrderAscendingResolvedFromRegistry_OrdersTheSource()
    {
        PropertySelector selector = SelectorDefaults.OrderAscending + "(Id)";

        var result = Evaluate<List<TestItem>, IEnumerable<TestItem>>(selector, SampleItems());

        Assert.Equal([1, 2, 3], result.Select(i => i.Id));
    }

    [Fact]
    public void Evaluate_GenericOrderDescendingResolvedFromRegistry_OrdersTheSource()
    {
        PropertySelector<List<TestItem>> selector = SelectorDefaults.OrderDescending + "(Id)";

        var result = Evaluate<List<TestItem>, IEnumerable<TestItem>>(selector, SampleItems());

        Assert.Equal([3, 2, 1], result.Select(i => i.Id));
    }

    [Fact]
    public void Evaluate_AggregationResolvedFromRegistry_CountsTheSource()
    {
        PropertySelector selector = SelectorDefaults.CountAggregateFunction;

        var result = Evaluate<List<TestItem>, int>(selector, SampleItems());

        Assert.Equal(3, result);
    }

    [Fact]
    public void Evaluate_RepeatedConversions_ProduceEquivalentExpressions()
    {
        // Factories are cached per closed type; the instances they hand out must stay
        // independent but behave identically.
        PropertySelector first = SelectorDefaults.OrderAscending + "(Id)";
        PropertySelector second = SelectorDefaults.OrderAscending + "(Id)";

        Assert.NotSame(first, second);
        Assert.Equal(
            Evaluate<List<TestItem>, IEnumerable<TestItem>>(first, SampleItems()).Select(i => i.Id),
            Evaluate<List<TestItem>, IEnumerable<TestItem>>(second, SampleItems()).Select(i => i.Id));
    }
}
