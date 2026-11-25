using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

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

    /// <summary>
    /// Walks the whole selector chain into an expression and runs it, which is the contract
    /// CreateMemberAccess exists for: one target in, one compiled accessor out.
    /// </summary>
    private static TResult Evaluate<TResult>(PropertySelector selector, TestPerson item)
    {
        var parameter = Expression.Parameter(typeof(TestPerson), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestPerson, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

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

    [Fact]
    public void CreateMemberAccess_WithSimplePropertySelector_ReadsTheProperty()
    {
        // Arrange
        PropertySelector selector = "Name";

        // Act
        var result = Evaluate<string>(selector, new TestPerson { Name = "Ada" });

        // Assert
        Assert.Equal("Ada", result);
    }

    [Fact]
    public void CreateMemberAccess_WithDottedPath_WalksThePath()
    {
        // Arrange - a dotted path over a collection projects it with Select
        PropertySelector selector = "Phones.Number";

        // Act
        var result = Evaluate<IEnumerable<string>>(selector, new TestPerson
        {
            Phones = [new TestPhone { Number = "1" }, new TestPhone { Number = "2" }],
        });

        // Assert
        Assert.Equal(["1", "2"], result);
    }

    [Fact]
    public void CreateMemberAccess_WalksEveryLinkOfTheChain()
    {
        // Arrange - three parts: property, function, function
        PropertySelector selector =
            $"Phones{SelectorDefaults.PropertyDelimiter}{SelectorDefaults.ElementFirst}"
            + $"{SelectorDefaults.PropertyDelimiter}Number";

        // Act
        var result = Evaluate<string>(selector, new TestPerson
        {
            Phones = [new TestPhone { Number = "1" }, new TestPhone { Number = "2" }],
        });

        // Assert
        Assert.Equal("1", result);
    }

    [Fact]
    public void CreateMemberAccess_WithEmptyPlainValue_ReturnsTarget()
    {
        // Arrange
        var param = Expression.Parameter(typeof(TestPerson), "x");
        PropertySelector selector = string.Empty;

        // Act
        var result = ExpressionUtils.CreateMemberAccess(param, selector);

        // Assert
        Assert.Same(param, result);
    }

    #endregion

    #region FindRootParameter

    public static TheoryData<string> RootedSelectors => new()
    {
        "Name",
        "Phones.Number",
        "Name|$FN_STR_TRIM",
        "Nickname|$FN_COALESCE(\"x\")",
        "$FN_MATH_MAX(5, Id)",
        "$FN_STR_JOIN(\",\", \"a\", Name)",
        "Id + 1",
        "1 + Id",
    };

    [Theory]
    [MemberData(nameof(RootedSelectors))]
    public void FindRootParameter_FindsTheRootUnderAnyOperand(string text)
    {
        var param = Expression.Parameter(typeof(TestPerson), "x");

        Assert.Same(param, ExpressionUtils.FindRootParameter(ExpressionUtils.CreateMemberAccess(param, (PropertySelector)text)));
        Assert.Same(param, ExpressionUtils.FindRootParameter(ExpressionUtils.CreateMemberAccess(param, (PropertySelector<TestPerson>)text)));
    }

    [Fact]
    public void FindRootParameter_DoesNotTakeAnInnerLambdaParameter()
    {
        var inner = Expression.Parameter(typeof(TestPhone), "p");
        var lambda = Expression.Lambda(Expression.Property(inner, nameof(TestPhone.Id)), inner);

        Assert.Null(ExpressionUtils.FindRootParameter(lambda));
        Assert.Null(ExpressionUtils.FindRootParameter(Expression.Constant(1)));
    }

    [Theory]
    [InlineData("Nickname|$FN_COALESCE(\"x\")|$FN_STR_CONTAINS(Name)", false)]
    [InlineData("Nickname|$FN_COALESCE(Name)|$FN_STR_CONTAINS(Name)", true)]
    public void AFunctionChainedAfterCoalesce_ResolvesItsArgumentsAgainstTheRoot(string text, bool expected)
    {
        var param = Expression.Parameter(typeof(TestPerson), "x");
        var person = new TestPerson { Name = "Ann" };

        foreach (var selector in new[] { (PropertySelector)text, (PropertySelector<TestPerson>)text })
        {
            var body = ExpressionUtils.CreateMemberAccess(param, selector);
            Assert.Equal(expected, Expression.Lambda<Func<TestPerson, bool>>(body, param).Compile()(person));
        }
    }

    #endregion

    #region Test Helper Classes

    private class TestPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public List<TestPhone> Phones { get; set; } = [];
    }

    private class TestPhone
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
    }

    #endregion
}