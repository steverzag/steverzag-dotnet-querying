using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.AggregationSelectors;

/// <summary>
/// Covers the aggregation selector family end to end: the string is parsed into a selector
/// chain, the chain is turned into an expression and the expression is executed.
/// </summary>
public class AggregationSelectorTests
{
    private class TestLine
    {
        public int Amount { get; set; }
        public double Weight { get; set; }
    }

    private class TestOrder
    {
        public List<TestLine> Lines { get; set; } = [];
        public List<int> Numbers { get; set; } = [];
        public List<double> Weights { get; set; } = [];
        public string Code { get; set; } = string.Empty;
    }

    // Aggregations take their source from the chained target, so the whole selector chain has
    // to be walked (a single PropertyAccess call only covers one part).
    private static TResult Evaluate<TResult>(PropertySelector<TestOrder> selector, TestOrder item)
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestOrder, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector, TestOrder item)
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestOrder, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private const string Delimiter = SelectorDefaults.PropertyDelimiter;

    private static TestOrder SampleOrder() => new()
    {
        Lines =
        [
            new TestLine { Amount = 30, Weight = 1.5 },
            new TestLine { Amount = 10, Weight = 2.5 },
            new TestLine { Amount = 20, Weight = 3.0 },
        ],
        Numbers = [10, 20, 30],
        Weights = [1.5, 2.5],
        Code = "abc",
    };

    // ── Count ───────────────────────────────────────────────────────────────

    [Fact]
    public void Count_OnCollectionProperty_ReturnsElementCount()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(3, result);
    }

    [Fact]
    public void Count_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, new TestOrder());

        Assert.Equal(0, result);
    }

    [Fact]
    public void Count_OnComplexElementCollection_ReturnsElementCount()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(3, result);
    }

    [Fact]
    public void Count_OnNonGenericSelector_ReturnsElementCount()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(3, result);
    }

    [Fact]
    public void Count_OnNonEnumerableProperty_ThrowsInvalidOperationExceptionAtParseTime()
    {
        // The generic selector validates enumerability in its constructor.
        Assert.Throws<InvalidOperationException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Code{Delimiter}{SelectorDefaults.CountAggregateFunction}";
        });
    }

    [Fact]
    public void Count_WithParameters_ThrowsNotSupportedException()
    {
        // Aggregation selectors only declare a parameterless constructor.
        Assert.Throws<NotSupportedException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}(Amount)";
        });
    }

    // ── LongCount ───────────────────────────────────────────────────────────

    [Fact]
    public void LongCount_OnCollectionProperty_ReturnsElementCountAsLong()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        var result = Evaluate<long>(selector, SampleOrder());

        Assert.Equal(typeof(long), selector.Type);
        Assert.Equal(3L, result);
    }

    [Fact]
    public void LongCount_EmitsALongCountThatMatchesTheDeclaredSelectorType()
    {
        // Regression guard: the implementation used to bind Enumerable.Count, so the compiled
        // expression yielded an int while SelectorType declared long.
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);

        Assert.Equal(typeof(long), body.Type);
        Assert.Equal(selector.Type, body.Type);
    }

    [Fact]
    public void LongCount_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        Assert.Equal(0L, Evaluate<long>(selector, new TestOrder()));
    }

    [Fact]
    public void LongCount_OnNonGenericSelector_ReturnsElementCountAsLong()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        Assert.Equal(3L, Evaluate<long>(selector, SampleOrder()));
    }

    // ── Sum ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Sum_OnIntegerCollection_ReturnsTotal()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(60, result);
    }

    [Fact]
    public void Sum_OnProjectedCollectionProperty_SumsTheProjectedValues()
    {
        // "Lines.Amount" projects the collection with Select before the aggregation runs.
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(60, result);
    }

    [Fact]
    public void Sum_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, new TestOrder());

        Assert.Equal(0, result);
    }

    [Fact]
    public void Sum_OnNonIntegerCollection_ThrowsOnBuild()
    {
        // The aggregation implementations bind the non-generic Enumerable.Sum(IEnumerable<int>)
        // overload, so any other element type fails when the call is built.
        PropertySelector<TestOrder> selector = $"Weights{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        Assert.ThrowsAny<ArgumentException>(() => Evaluate<double>(selector, SampleOrder()));
    }

    [Fact]
    public void Sum_OnNonGenericSelector_ReturnsTotal()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(60, result);
    }

    // ── Min / Max ───────────────────────────────────────────────────────────

    [Fact]
    public void Min_OnIntegerCollection_ReturnsSmallestValue()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.MinAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Max_OnIntegerCollection_ReturnsLargestValue()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.MaxAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(30, result);
    }

    [Fact]
    public void Max_OnProjectedCollectionProperty_ReturnsLargestProjectedValue()
    {
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.MaxAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(30, result);
    }

    [Fact]
    public void Min_OnNonGenericSelector_ReturnsSmallestValue()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.MinAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(10, result);
    }

    // ── Average ─────────────────────────────────────────────────────────────

    [Fact]
    public void Avg_OnIntegerCollection_ReturnsDoubleAverage()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var result = Evaluate<double>(selector, SampleOrder());

        Assert.Equal(typeof(double), selector.Type);
        Assert.Equal(20d, result);
    }

    [Fact]
    public void Avg_OnProjectedCollectionProperty_ReturnsDoubleAverage()
    {
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var result = Evaluate<double>(selector, SampleOrder());

        Assert.Equal(20d, result);
    }

    [Fact]
    public void Avg_OnNonGenericSelector_ReturnsDoubleAverage()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var result = Evaluate<double>(selector, SampleOrder());

        Assert.Equal(20d, result);
    }

    // ── Combined with the other families ────────────────────────────────────

    [Fact]
    public void Count_AfterOrdering_CountsTheOrderedSequence()
    {
        PropertySelector<TestOrder> selector =
            $"Numbers{Delimiter}{SelectorDefaults.OrderAscending}{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(3, result);
    }
}
