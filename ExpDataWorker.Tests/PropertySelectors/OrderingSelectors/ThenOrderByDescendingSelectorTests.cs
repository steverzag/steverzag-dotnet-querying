using ExpDataWorker.PropertySelectors.OrderingSelectors;
using ExpDataWorker.PropertySelectors;
using System.Linq.Expressions;

namespace ExpDataWorker.Tests.PropertySelectors.OrderingSelectors;

public class ThenOrderByDescendingSelectorTests
{
    private class TestLine
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    private class TestOrder
    {
        public List<TestLine> Lines { get; set; } = [];
    }

    // ThenByDescending only accepts an already ordered source, so every evaluation walks a
    // chain that starts with an OrderBy part.
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

    // Two lines share a name so the secondary key decides their relative order.
    private static TestOrder SampleOrder() => new()
    {
        Lines =
        [
            new TestLine { Id = 1, Name = "a", Amount = 30 },
            new TestLine { Id = 2, Name = "b", Amount = 10 },
            new TestLine { Id = 3, Name = "a", Amount = 20 },
        ],
    };

    [Fact]
    public void Constructor_WithNoParameters_InitializesCorrectly()
    {
        // Act
        var selector = new ThenOrderByDescendingSelector();

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Empty(selector.Parameters);
        Assert.Single(selector.Overloads);
    }

    [Fact]
    public void Constructor_WithNoParameters_SetsCorrectPlainValue()
    {
        // Act
        var selector = new ThenOrderByDescendingSelector();

        // Assert
        Assert.StartsWith(SelectorDefaults.ThenOrderByDescending, selector.PlainValue);
        Assert.Contains("()", selector.PlainValue);
    }

    [Fact]
    public void Constructor_WithOneParameter_InitializesCorrectly()
    {
        // Arrange
        PropertySelector innerSelector = "Name";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new ThenOrderByDescendingSelector(parameters);

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Single(selector.Parameters);
        Assert.Single(selector.Overloads);
    }

    [Fact]
    public void Constructor_WithOneParameter_SetsCorrectPlainValue()
    {
        // Arrange
        PropertySelector innerSelector = "Name";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new ThenOrderByDescendingSelector(parameters);

        // Assert
        Assert.StartsWith(SelectorDefaults.ThenOrderByDescending, selector.PlainValue);
        Assert.Contains("(Name)", selector.PlainValue);
    }

    [Fact]
    public void InitializeOverloads_WithNoParameters_CreatesEmptyParameterOverload()
    {
        // Act
        var selector = new ThenOrderByDescendingSelector();

        // Assert
        Assert.Single(selector.Overloads);
        Assert.Empty(selector.Overloads[0].Parameters);
    }

    [Fact]
    public void InitializeOverloads_WithOneParameter_CreatesSingleParameterOverload()
    {
        // Arrange
        PropertySelector innerSelector = "Name";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new ThenOrderByDescendingSelector(parameters);

        // Assert
        Assert.Single(selector.Overloads);
        Assert.Single(selector.Overloads[0].Parameters);
        Assert.Equal(typeof(PropertySelector), selector.Overloads[0].Parameters[0]);
    }

    [Fact]
    public void Constructor_WithInvalidParameterCount_ThrowsArgumentException()
    {
        // Arrange
        PropertySelector param1 = "Id";
        PropertySelector param2 = "Name";
        var parameters = new List<PropertySelector> { param1, param2 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ThenOrderByDescendingSelector(parameters));
    }

    // ── Parsing and evaluation ──────────────────────────────────────────────

    [Fact]
    public void ThenOrderDescending_AfterOrderAscending_BreaksTiesWithTheSecondaryKey()
    {
        PropertySelector<TestOrder> selector =
            $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Name){Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)";

        var result = Evaluate<IEnumerable<TestLine>>(selector, SampleOrder());

        Assert.Equal([1, 3, 2], result.Select(l => l.Id));
    }

    [Fact]
    public void ThenOrderDescending_AfterOrderDescending_KeepsThePrimaryKeyOrder()
    {
        PropertySelector<TestOrder> selector =
            $"Lines{Delimiter}{SelectorDefaults.OrderDescending}(Name){Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)";

        var result = Evaluate<IEnumerable<TestLine>>(selector, SampleOrder());

        Assert.Equal([2, 1, 3], result.Select(l => l.Id));
    }

    [Fact]
    public void ThenOrderDescending_OnNonGenericSelector_BreaksTiesWithTheSecondaryKey()
    {
        PropertySelector selector =
            $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Name){Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)";

        var result = Evaluate<IEnumerable<TestLine>>(selector, SampleOrder());

        Assert.Equal([1, 3, 2], result.Select(l => l.Id));
    }

    [Fact]
    public void ThenOrderDescending_ChainedWithFirstElement_ReturnsTheWinnerOfBothKeys()
    {
        PropertySelector<TestOrder> selector =
            $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Name)"
            + $"{Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)"
            + $"{Delimiter}{SelectorDefaults.ElementFirst}";

        var result = Evaluate<TestLine>(selector, SampleOrder());

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void ThenOrderDescending_SetsSelectorTypeToOrderedSequenceOfTheElementType()
    {
        PropertySelector<TestOrder> selector =
            $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Name){Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)";

        Assert.Equal(typeof(IOrderedQueryable<TestLine>), selector.Type);
    }

    [Fact]
    public void ThenOrderDescending_WithoutAPrecedingOrderBy_ThrowsOnBuild()
    {
        // ThenByDescending is typed on IOrderedEnumerable; a plain list target only fails when
        // the expression is built, not at parse time.
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)";

        Assert.ThrowsAny<ArgumentException>(() => Evaluate<IEnumerable<TestLine>>(selector, SampleOrder()));
    }
}