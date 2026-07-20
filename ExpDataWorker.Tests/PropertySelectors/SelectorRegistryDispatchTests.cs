using ExpDataWorker.PropertySelectors.ArithmeticSelectors;
using ExpDataWorker.PropertySelectors.OrderingSelectors;
using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors;

namespace ExpDataWorker.Tests.PropertySelectors;

public class SelectorRegistryDispatchTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

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
    public void ImplicitOperator_UnknownFunctionToken_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => (PropertySelector)"$FN_NOPE(1)");
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
    public void ImplicitOperator_FunctionWithUnsupportedParameterCount_ThrowsNotSupportedException()
    {
        // Aggregation selectors only define a parameterless constructor.

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => (PropertySelector)(SelectorDefaults.SumAggregateFunction + "(Id)"));
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
}
