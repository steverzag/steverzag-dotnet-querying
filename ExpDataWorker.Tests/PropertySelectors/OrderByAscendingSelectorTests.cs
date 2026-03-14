using ExpDataWorker.PropertySelector.OrderingSelectors;

namespace ExpDataWorker.Tests.PropertySelectors;

public class OrderByAscendingSelectorTests
{
    [Fact]
    public void Constructor_WithNoParameters_InitializesCorrectly()
    {
        // Act
        var selector = new OrderByAscendingSelector();

        // Assert
        Assert.NotNull(selector);
        //Assert.NotNull(selector.CreateAccessor);
        Assert.Empty(selector.Parameters);
    }

    [Fact]
    public void Constructor_WithParameters_InitializesCorrectly()
    {
        // Arrange
        var parameters = new List<ExpDataWorker.PropertySelector.PropertySelector>();

        // Act
        var selector = new OrderByAscendingSelector(parameters);

        // Assert
        Assert.NotNull(selector);
        //Assert.NotNull(selector.CreateAccessor);
        Assert.Same(parameters, selector.Parameters);
    }

    [Fact]
    public void InitializeOverloads_AddsExpectedOverloads()
    {
        // Arrange
        var selector = new OrderByAscendingSelector();

        // Act
        selector.InitializeOverloads();

        // Assert
        Assert.NotEmpty(selector.Overloads);
        Assert.Equal(2, selector.Overloads.Count); // Empty params + ValueSpecifier param
    }

    [Fact]
    public void Overloads_ContainsEmptyParameterOverload()
    {
        // Arrange
        var selector = new OrderByAscendingSelector();
        selector.InitializeOverloads();

        // Act
        var emptyOverload = selector.Overloads.FirstOrDefault(o => o.Parameters.Count == 0);

        // Assert
        Assert.NotNull(emptyOverload);
    }

    [Fact]
    public void Overloads_ContainsSingleParameterOverload()
    {
        // Arrange
        var selector = new OrderByAscendingSelector();
        selector.InitializeOverloads();

        // Act
        var singleParamOverload = selector.Overloads.FirstOrDefault(o => o.Parameters.Count == 1);

        // Assert
        Assert.NotNull(singleParamOverload);
    }
}