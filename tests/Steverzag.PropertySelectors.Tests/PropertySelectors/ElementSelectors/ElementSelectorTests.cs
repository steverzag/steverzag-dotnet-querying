using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.ElementSelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ElementSelectors;

/// <summary>
/// Covers the element selector family end to end: the string is parsed into a selector chain,
/// the chain is turned into an expression and the expression is executed.
/// </summary>
public class ElementSelectorTests
{
    private class TestLine
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestOrder
    {
        public List<TestLine> Lines { get; set; } = [];
        public List<int> Numbers { get; set; } = [];
        public string Code { get; set; } = string.Empty;
    }

    // Element functions take their source from the chained target, so the whole selector chain
    // has to be walked (a single PropertyAccess call only covers one part).
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
            new TestLine { Id = 1, Name = "first" },
            new TestLine { Id = 2, Name = "middle" },
            new TestLine { Id = 3, Name = "last" },
        ],
        Numbers = [10, 20, 30],
        Code = "abc",
    };

    // ── First ───────────────────────────────────────────────────────────────

    [Fact]
    public void ElementFirst_OnCollectionProperty_ReturnsFirstElement()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementFirst}";

        var result = Evaluate<TestLine>(selector, SampleOrder());

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void ElementFirst_WithEmptyParentheses_ReturnsFirstElement()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.ElementFirst}()";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(10, result);
    }

    [Fact]
    public void ElementFirst_SetsSelectorTypeToTheElementType()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementFirst}";

        Assert.Equal(typeof(TestLine), selector.Type);
    }

    [Fact]
    public void ElementFirst_ChainedWithAProperty_ReadsThePropertyOfTheElement()
    {
        // Only the non-generic operator supports this chain today — see
        // ElementFirst_ChainedWithAProperty_OnGenericSelector_ThrowsBecausePlainPartsResolveAgainstTheRoot.
        PropertySelector selector = $"Lines{Delimiter}{SelectorDefaults.ElementFirst}{Delimiter}Name";

        var result = Evaluate<string>(selector, SampleOrder());

        Assert.Equal("first", result);
    }

    [Fact]
    public void ElementFirst_ChainedWithAProperty_OnGenericSelector_ThrowsBecausePlainPartsResolveAgainstTheRoot()
    {
        // Known limitation of the generic operator: it threads the running element type into
        // the registry for function parts, but a plain property part is always built as
        // PropertySelector<T> against the ROOT type, so "Name" is looked up on TestOrder
        // instead of on the TestLine the preceding function returned.
        var exception = Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementFirst}{Delimiter}Name";
        });

        Assert.Contains(nameof(TestOrder), exception.Message);
    }

    [Fact]
    public void ElementFirst_OnNonGenericSelector_ReturnsFirstElement()
    {
        PropertySelector selector = $"Lines{Delimiter}{SelectorDefaults.ElementFirst}";

        var result = Evaluate<TestLine>(selector, SampleOrder());

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void ElementFirst_OnStringProperty_ThrowsSelectorParseException()
    {
        // string is deliberately excluded from the enumerable detection, so it is not a
        // sequence of characters as far as the element selectors are concerned.
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Code{Delimiter}{SelectorDefaults.ElementFirst}";
        });
    }

    // ── Last ────────────────────────────────────────────────────────────────

    [Fact]
    public void ElementLast_OnCollectionProperty_ReturnsLastElement()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementLast}";

        var result = Evaluate<TestLine>(selector, SampleOrder());

        Assert.Equal(3, result.Id);
    }

    [Fact]
    public void ElementLast_OnEmptyCollection_ReturnsDefault()
    {
        // The implementation calls LastOrDefault, so an empty source is not an error.
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.ElementLast}";

        var result = Evaluate<int>(selector, new TestOrder());

        Assert.Equal(0, result);
    }

    [Fact]
    public void ElementLast_SetsSelectorTypeToTheElementType()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementLast}";

        Assert.Equal(typeof(TestLine), selector.Type);
    }

    [Fact]
    public void ElementLast_OnNonGenericSelector_ReturnsLastElement()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.ElementLast}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(30, result);
    }

    // ── ElementAt ───────────────────────────────────────────────────────────

    [Fact]
    public void ElementAtIndex_WithBareIntegerLiteral_ReturnsElementAtThatIndex()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementAtIndex}(1)";

        var result = Evaluate<TestLine>(selector, SampleOrder());

        Assert.Equal(2, result.Id);
    }

    [Fact]
    public void ElementAtIndex_WithIntegerLiteral_ReturnsElementAtThatIndex()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.ElementAtIndex}(2)";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(30, result);
    }

    [Fact]
    public void ElementAtIndex_WithIndexPastTheEnd_ReturnsDefault()
    {
        // Skip(n) + FirstOrDefault() never throws for an out of range index.
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.ElementAtIndex}(9)";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(0, result);
    }

    [Fact]
    public void ElementAtIndex_ChainedWithAProperty_ReadsThePropertyOfTheElement()
    {
        // Non-generic for the same reason as ElementFirst_ChainedWithAProperty: plain parts of
        // a generic chain resolve against the root type.
        PropertySelector selector = $"Lines{Delimiter}{SelectorDefaults.ElementAtIndex}(2){Delimiter}Name";

        var result = Evaluate<string>(selector, SampleOrder());

        Assert.Equal("last", result);
    }

    [Fact]
    public void ElementAtIndex_OnNonGenericSelector_ReturnsElementAtThatIndex()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.ElementAtIndex}(1)";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(20, result);
    }

    [Fact]
    public void ElementAtIndex_WithNonIntegerIndex_IsRejectedWhenParsed()
    {
        // The index has to be an integer literal: it is read off the selector to build the
        // Skip() constant, so a property reference cannot supply it.
        var text = $"Lines{Delimiter}{SelectorDefaults.ElementAtIndex}(Code)";

        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<TestOrder>)text);
        Assert.Equal("$FN_ELM_ATINDEX: argument 1 ('Code') must be an integer literal: '$FN_ELM_ATINDEX(Code)'.", exception.Detail);
        Assert.Throws<SelectorParseException>(() => (PropertySelector)text);
    }

    [Fact]
    public void ElementAtIndex_WithoutParameters_ThrowsSelectorParseException()
    {
        // Only the one-parameter overload is registered.
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.ElementAtIndex}";
        });
    }
}
