using Steverzag.PropertySelectors.OrderingSelectors;
using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.OrderingSelectors;

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
        Assert.NotNull(selector.Signature);
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
        Assert.NotNull(selector.Signature);
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
    public void Constructor_WithNoParameters_ResolvesTheNoArgumentSignature()
    {
        // Act
        var selector = new ThenOrderByDescendingSelector();

        // Assert
        Assert.NotNull(selector.Signature);
        Assert.Empty(selector.Signature!.Parameters);
    }

    [Fact]
    public void Constructor_WithOneParameter_ResolvesTheOneArgumentSignature()
    {
        // Arrange
        PropertySelector innerSelector = "Name";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new ThenOrderByDescendingSelector(parameters);

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

        Assert.Equal(typeof(IOrderedEnumerable<TestLine>), selector.Type);
        Assert.Equal(ExpressionUtils.CreateMemberAccess(System.Linq.Expressions.Expression.Parameter(typeof(TestOrder), "o"), selector).Type, selector.Type);
    }

    [Fact]
    public void ThenOrderDescending_WithoutAPrecedingOrderBy_IsRejectedWhenParsed()
    {
        var text = $"Lines{Delimiter}{SelectorDefaults.ThenOrderByDescending}(Amount)";

        var generic = Assert.Throws<SelectorParseException>(() => (PropertySelector<TestOrder>)text);
        var nonGeneric = Assert.Throws<SelectorParseException>(() => Evaluate<IEnumerable<TestLine>>((PropertySelector)text, SampleOrder()));

        Assert.Contains("must be applied to an ordered collection (sorted first with $FN_ORDER_ASC or $FN_ORDER_DESC)", generic.Detail);
        Assert.Equal(generic.Detail, nonGeneric.Detail);
    }
}