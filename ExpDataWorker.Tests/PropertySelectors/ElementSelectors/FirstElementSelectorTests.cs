using ExpDataWorker.PropertySelectors;
using ExpDataWorker.PropertySelectors.ElementSelectors;

namespace ExpDataWorker.Tests.PropertySelectors.ElementSelectors;

public class FirstElementSelectorTests
{
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
        Assert.Single(selector.Overloads);
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
        Assert.Single(selector.Overloads);
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
    public void NonGeneric_InitializeOverloads_RegistersSingleOverload()
    {
        // Act
        var selector = new FirstElementSelector();

        // Assert
        Assert.Single(selector.Overloads);
        Assert.Empty(selector.Overloads[0].Parameters);
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
        Assert.Single(selector.Overloads);
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
    public void Generic_InitializeOverloads_RegistersSingleOverload()
    {
        // Act
        var selector = new FirstElementSelector<IEnumerable<int>>();

        // Assert
        Assert.Single(selector.Overloads);
        Assert.Empty(selector.Overloads[0].Parameters);
    }
}
