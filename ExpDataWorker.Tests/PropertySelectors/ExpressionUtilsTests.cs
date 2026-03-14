
using ExpDataWorker.PropertySelector;
using System.Linq.Expressions;

namespace ExpDataWorker.Tests.PropertySelectors;

public class ExpressionUtilsTests
{
    #region TryGetEnumerableElementType Tests

    [Fact]
    public void TryGetEnumerableElementType_WithListOfInt_ReturnsTrue()
    {
        // Arrange
        var type = typeof(List<int>);

        // Act
        var result = ExpressionUtils.TryGetEnumerableElementType(type, out var elementType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(int), elementType);
    }

    [Fact]
    public void TryGetEnumerableElementType_WithIEnumerableOfString_ReturnsTrue()
    {
        // Arrange
        var type = typeof(IEnumerable<string>);

        // Act
        var result = ExpressionUtils.TryGetEnumerableElementType(type, out var elementType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(string), elementType);
    }

    [Fact]
    public void TryGetEnumerableElementType_WithArray_ReturnsTrue()
    {
        // Arrange
        var type = typeof(int[]);

        // Act
        var result = ExpressionUtils.TryGetEnumerableElementType(type, out var elementType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(int), elementType);
    }

    [Fact]
    public void TryGetEnumerableElementType_WithString_ReturnsFalse()
    {
        // Arrange - string implements IEnumerable<char> but should be excluded
        var type = typeof(string);

        // Act
        var result = ExpressionUtils.TryGetEnumerableElementType(type, out var elementType);

        // Assert
        Assert.False(result);
        Assert.Null(elementType);
    }

    [Fact]
    public void TryGetEnumerableElementType_WithNonEnumerable_ReturnsFalse()
    {
        // Arrange
        var type = typeof(int);

        // Act
        var result = ExpressionUtils.TryGetEnumerableElementType(type, out var elementType);

        // Assert
        Assert.False(result);
        Assert.Null(elementType);
    }

    #endregion

    #region TryGetGroupingTypes Tests

    [Fact]
    public void TryGetGroupingTypes_WithIGrouping_ReturnsTrue()
    {
        // Arrange
        var type = typeof(IGrouping<string, int>);

        // Act
        var result = ExpressionUtils.TryGetGroupingTypes(type, out var keyType, out var elementType);

        // Assert
        Assert.True(result);
        Assert.Equal(typeof(string), keyType);
        Assert.Equal(typeof(int), elementType);
    }

    [Fact]
    public void TryGetGroupingTypes_WithNonGrouping_ReturnsFalse()
    {
        // Arrange
        var type = typeof(List<int>);

        // Act
        var result = ExpressionUtils.TryGetGroupingTypes(type, out var keyType, out var elementType);

        // Assert
        Assert.False(result);
        Assert.Null(keyType);
        Assert.Null(elementType);
    }

    [Fact]
    public void TryGetGroupingTypes_WithString_ReturnsFalse()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = ExpressionUtils.TryGetGroupingTypes(type, out var keyType, out var elementType);

        // Assert
        Assert.False(result);
        Assert.Null(keyType);
        Assert.Null(elementType);
    }

    #endregion

    #region CreateMemberAccess Tests

    [Fact]
    public void CreateMemberAccess_WithNullSelector_ReturnsTarget()
    {
        // Arrange
        var param = Expression.Parameter(typeof(TestPerson), "x");

        // Act
        var result = ExpressionUtils.CreateMemberAccess(param, null!);

        // Assert
        Assert.Same(param, result);
    }

    #endregion

    #region Test Helper Classes

    private class TestPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TestPhone> Phones { get; set; } = [];
    }

    private class TestPhone
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
    }

    #endregion
}