using ExpDataWorker.PropertySelector;
using ExpDataWorker.PropertySelector.OrderingSelectors;

namespace ExpDataWorker.Tests.PropertySelectors;

public class ThenOrderByAscendingSelectorTests
{
    [Fact]
    public void Constructor_WithNoParameters_InitializesCorrectly()
    {
        // Act
        var selector = new ThenOrderByAscendingSelector();

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
        var selector = new ThenOrderByAscendingSelector();

        // Assert
        Assert.StartsWith(SelectorDefaults.OrderAscending, selector.PlainValue);
        Assert.Contains("()", selector.PlainValue);
    }

    [Fact]
    public void Constructor_WithOneParameter_InitializesCorrectly()
    {
        // Arrange
        PropertySelector innerSelector = "Name";
        var parameters = new List<PropertySelector> { innerSelector };

        // Act
        var selector = new ThenOrderByAscendingSelector(parameters);

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
        var selector = new ThenOrderByAscendingSelector(parameters);

        // Assert
        Assert.StartsWith(SelectorDefaults.OrderAscending, selector.PlainValue);
        Assert.Contains("(Name)", selector.PlainValue);
    }

    [Fact]
    public void InitializeOverloads_WithNoParameters_CreatesEmptyParameterOverload()
    {
        // Act
        var selector = new ThenOrderByAscendingSelector();

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
        var selector = new ThenOrderByAscendingSelector(parameters);

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
        Assert.Throws<ArgumentException>(() => new ThenOrderByAscendingSelector(parameters));
    }
}