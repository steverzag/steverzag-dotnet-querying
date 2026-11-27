using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.ElementSelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ElementSelectors;

public class FirstElementSelectorTests
{
    /// <summary>
    /// Element selectors apply straight to the sequence they are handed, so the source itself
    /// is the target expression here (no property path in front of it).
    /// </summary>
    private static TResult Evaluate<TSource, TResult>(PropertySelector selector, TSource source)
    {
        var parameter = Expression.Parameter(typeof(TSource), "x");
        var body = SelectorExpressions.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TSource, TResult>>(body, parameter).Compile();
        return lambda(source);
    }

    // ── Non-generic FirstElementSelector ──────────────────────────────────────

    [Fact]
    public void NonGeneric_Constructor_WithNoParameters_InitializesCorrectly()
    {
        // Act
        var selector = new FirstElementSelector();

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Empty(selector.Parameters);
        Assert.NotNull(selector.Signature);
    }

    [Fact]
    public void NonGeneric_Constructor_WithNoParameters_SetsCorrectPlainValue()
    {
        // Act
        var selector = new FirstElementSelector();

        // Assert
        Assert.StartsWith(SelectorDefaults.ElementFirst, selector.PlainValue);
        Assert.Contains("()", selector.PlainValue);
    }

    [Fact]
    public void NonGeneric_Constructor_WithEmptyParameterList_InitializesCorrectly()
    {
        // Arrange
        var parameters = new List<PropertySelector>();

        // Act
        var selector = new FirstElementSelector(parameters);

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Empty(selector.Parameters);
        Assert.NotNull(selector.Signature);
    }

    [Fact]
    public void NonGeneric_Constructor_WithEmptyParameterList_SetsCorrectPlainValue()
    {
        // Arrange
        var parameters = new List<PropertySelector>();

        // Act
        var selector = new FirstElementSelector(parameters);

        // Assert
        Assert.StartsWith(SelectorDefaults.ElementFirst, selector.PlainValue);
        Assert.Contains("()", selector.PlainValue);
    }

    [Fact]
    public void NonGeneric_Constructor_WithNonEmptyParameterList_ThrowsArgumentException()
    {
        // Arrange
        PropertySelector param = "Id";
        var parameters = new List<PropertySelector> { param };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FirstElementSelector(parameters));
    }

    [Fact]
    public void NonGeneric_Constructor_ResolvesTheNoArgumentSignature()
    {
        // Act
        var selector = new FirstElementSelector();

        // Assert
        Assert.NotNull(selector.Signature);
        Assert.Empty(selector.Signature!.Parameters);
    }

    // ── Generic FirstElementSelector<T> ──────────────────────────────────────

    [Fact]
    public void Generic_Constructor_WithEnumerableType_InitializesCorrectly()
    {
        // Act
        var selector = new FirstElementSelector<IEnumerable<int>>();

        // Assert
        Assert.NotNull(selector);
        Assert.NotNull(selector.PropertyAccess);
        Assert.Empty(selector.Parameters);
        Assert.NotNull(selector.Signature);
    }

    [Fact]
    public void Generic_Constructor_WithEnumerableType_SetsCorrectPlainValue()
    {
        // Act
        var selector = new FirstElementSelector<IEnumerable<int>>();

        // Assert
        Assert.StartsWith(SelectorDefaults.ElementFirst, selector.PlainValue);
        Assert.Contains("()", selector.PlainValue);
    }

    [Fact]
    public void Generic_Constructor_WithIEnumerableOfInt_SetsSelectorTypeToElementType()
    {
        // Act
        var selector = new FirstElementSelector<IEnumerable<int>>();

        // Assert
        Assert.Equal(typeof(int), selector.SelectorType);
    }

    [Fact]
    public void Generic_Constructor_WithListOfString_SetsSelectorTypeToElementType()
    {
        // Act
        var selector = new FirstElementSelector<List<string>>();

        // Assert
        Assert.Equal(typeof(string), selector.SelectorType);
    }

    [Fact]
    public void Generic_Constructor_WithNonEnumerableType_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new FirstElementSelector<int>());
    }

    [Fact]
    public void Generic_Constructor_ResolvesTheNoArgumentSignature()
    {
        // Act
        var selector = new FirstElementSelector<IEnumerable<int>>();

        // Assert
        Assert.NotNull(selector.Signature);
        Assert.Empty(selector.Signature!.Parameters);
    }

    // ── Evaluation ──────────────────────────────────────────────────────────

    [Fact]
    public void Generic_Evaluate_OnFilledSequence_ReturnsFirstElement()
    {
        var selector = new FirstElementSelector<List<int>>();

        var result = Evaluate<List<int>, int>(selector, [3, 1, 2]);

        Assert.Equal(3, result);
    }

    [Fact]
    public void Generic_Evaluate_OnEmptySequence_ReturnsDefault()
    {
        // The implementation calls FirstOrDefault, so an empty source is not an error.
        var selector = new FirstElementSelector<List<int>>();

        var result = Evaluate<List<int>, int>(selector, []);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Generic_Evaluate_OnReferenceElementSequence_ReturnsNullWhenEmpty()
    {
        var selector = new FirstElementSelector<List<string>>();

        var result = Evaluate<List<string>, string?>(selector, []);

        Assert.Null(result);
    }

    [Fact]
    public void NonGeneric_Evaluate_OnFilledSequence_ReturnsFirstElement()
    {
        var selector = new FirstElementSelector();

        var result = Evaluate<List<string>, string>(selector, ["a", "b"]);

        Assert.Equal("a", result);
    }

    [Fact]
    public void NonGeneric_Evaluate_OnNonEnumerableTarget_ThrowsSelectorParseException()
    {
        // The non-generic selector has no type to validate at construction time, so the
        // enumerability check only happens when the expression is built.
        var selector = new FirstElementSelector();

        Assert.Throws<SelectorParseException>(() => Evaluate<int, int>(selector, 1));
    }
}
