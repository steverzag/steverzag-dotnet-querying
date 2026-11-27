using Steverzag.PropertySelectors.OrderingSelectors;
using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.OrderingSelectors;

public class OrderByAscendingSelectorTests
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
        public List<int> Numbers { get; set; } = [];
    }

    // Ordering selectors take their source from the chained target, so the whole selector
    // chain has to be walked (a single PropertyAccess call only covers one part).
    private static TResult Evaluate<TResult>(PropertySelector<TestOrder> selector, TestOrder item)
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = SelectorExpressions.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestOrder, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector, TestOrder item)
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = SelectorExpressions.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestOrder, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private const string Delimiter = SelectorDefaults.PropertyDelimiter;

    private static TestOrder SampleOrder() => new()
    {
        Lines =
        [
            new TestLine { Id = 1, Name = "b", Amount = 30 },
            new TestLine { Id = 2, Name = "a", Amount = 10 },
            new TestLine { Id = 3, Name = "c", Amount = 20 },
        ],
        Numbers = [3, 1, 2],
    };

    [Fact]
    public void Constructor_WithNoParameters_InitializesCorrectly()
    {
        // Act
        var selector = new OrderByAscendingSelector();

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Empty(selector.Parameters);
        Assert.NotNull(selector.Signature);
    }

    [Fact]
    public void Constructor_WithNoParameters_SetsCorrectPlainValue()
    {
        // Act
        var selector = new OrderByAscendingSelector();

        // Assert
        Assert.StartsWith(SelectorDefaults.OrderAscending, selector.PlainValue);
        Assert.Contains("()", selector.PlainValue);
    }

    [Fact]
    public void Constructor_WithOneParameter_InitializesCorrectly()
    {
        // Arrange
        PropertySelector innerSelector = "Id";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new OrderByAscendingSelector(parameters);

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Single(selector.Parameters);
        Assert.NotNull(selector.Signature);
    }

    [Fact]
    public void Constructor_WithOneParameter_SetsCorrectPlainValue()
    {
        // Arrange
        PropertySelector innerSelector = "Id";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new OrderByAscendingSelector(parameters);

        // Assert
        Assert.StartsWith(SelectorDefaults.OrderAscending, selector.PlainValue);
        Assert.Contains("(Id)", selector.PlainValue);
    }

    [Fact]
    public void Constructor_WithNoParameters_ResolvesTheNoArgumentSignature()
    {
        // Act
        var selector = new OrderByAscendingSelector();

        // Assert
        Assert.NotNull(selector.Signature);
        Assert.Empty(selector.Signature!.Parameters);
    }

    [Fact]
    public void Constructor_WithOneParameter_ResolvesTheOneArgumentSignature()
    {
        // Arrange
        PropertySelector innerSelector = "Id";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new OrderByAscendingSelector(parameters);

        // Assert
        Assert.NotNull(selector.Signature);
        Assert.Single(selector.Signature!.Parameters);
    }

    [Fact]
    public void Constructor_WithInvalidParameterCount_ThrowsArgumentException()
    {
        // Arrange
        PropertySelector param1 = "Id";
        PropertySelector param2 = "Name";
        var parameters = new List<PropertySelector> { param1, param2 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OrderByAscendingSelector(parameters));
    }

    // ── Parsing and evaluation ──────────────────────────────────────────────

    [Fact]
    public void OrderAscending_WithPropertyParameter_OrdersBySelectedProperty()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Amount)";

        var result = Evaluate<IEnumerable<TestLine>>(selector, SampleOrder());

        Assert.Equal([10, 20, 30], result.Select(l => l.Amount));
    }

    [Fact]
    public void OrderAscending_WithStringPropertyParameter_OrdersByStringComparison()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Name)";

        var result = Evaluate<IEnumerable<TestLine>>(selector, SampleOrder());

        Assert.Equal(["a", "b", "c"], result.Select(l => l.Name));
    }

    [Fact]
    public void OrderAscending_WithoutParameters_OrdersElementsByThemselves()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.OrderAscending}";

        var result = Evaluate<IEnumerable<int>>(selector, SampleOrder());

        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void OrderAscending_OnNonGenericSelector_OrdersBySelectedProperty()
    {
        PropertySelector selector = $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Amount)";

        var result = Evaluate<IEnumerable<TestLine>>(selector, SampleOrder());

        Assert.Equal([10, 20, 30], result.Select(l => l.Amount));
    }

    [Fact]
    public void OrderAscending_ChainedWithFirstElement_ReturnsSmallestElement()
    {
        PropertySelector<TestOrder> selector =
            $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Amount){Delimiter}{SelectorDefaults.ElementFirst}";

        var result = Evaluate<TestLine>(selector, SampleOrder());

        Assert.Equal(10, result.Amount);
    }

    [Fact]
    public void OrderAscending_SetsSelectorTypeToOrderedSequenceOfTheElementType()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.OrderAscending}(Amount)";

        Assert.Equal(typeof(IOrderedEnumerable<TestLine>), selector.Type);
        Assert.Equal(SelectorExpressions.CreateMemberAccess(System.Linq.Expressions.Expression.Parameter(typeof(TestOrder), "o"), selector).Type, selector.Type);
    }

    [Fact]
    public void OrderAscending_OnNonEnumerableTarget_ThrowsSelectorParseException()
    {
        // The generic selector validates the enumerability of its target at parse time.
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector =
                $"Lines{Delimiter}{SelectorDefaults.ElementFirst}{Delimiter}{SelectorDefaults.OrderAscending}(Amount)";
        });
    }
}