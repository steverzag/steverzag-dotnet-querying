using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ValueSelectors;

public class QuotedLiteralTests
{
    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
    }

    private static object? Evaluate(PropertySelector selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = Expression.Convert(ExpressionUtils.CreateMemberAccess(parameter, selector), typeof(object));
        return Expression.Lambda<Func<TestItem, object?>>(body, parameter).Compile()(item);
    }

    private static void AssertBothForms(string selector, TestItem item, object? expected)
    {
        Assert.Equal(expected, Evaluate((PropertySelector<TestItem>)selector, item));
        Assert.Equal(expected, Evaluate((PropertySelector)selector, item));
    }

    [Theory]
    [InlineData("\"n/a\"", "n/a")]
    [InlineData("\"a+b-c*d\"", "a+b-c*d")]
    [InlineData("\"a|b\"", "a|b")]
    [InlineData("\"(x\"", "(x")]
    [InlineData("\"x)\"", "x)")]
    [InlineData("\"a, b\"", "a, b")]
    public void StructuralCharactersInsideQuotes_StayInTheLiteral(string selector, string expected)
    {
        AssertBothForms(selector, new TestItem(), expected);
    }

    [Theory]
    [InlineData("Name|$FN_STR_CONTAINS(\"a,b\")", "xa,by", true)]
    [InlineData("Name|$FN_STR_CONTAINS(\"a|b\")", "a|b", true)]
    [InlineData("Name|$FN_STR_REPLACE(\"l\", \"-\")", "hello", "he--o")]
    [InlineData("Name|$FN_STR_REPLACE(\"/\", \", \")", "a/b", "a, b")]
    [InlineData("$FN_STR_CONCAT(\"(\", Name, \")\")", "hello", "(hello)")]
    public void QuotedFunctionArguments_AreNotSplit(string selector, string name, object expected)
    {
        AssertBothForms(selector, new TestItem { Name = name }, expected);
    }

    [Theory]
    [InlineData("Name|$FN_STR_INDEXOF(\"-\") + 1", "a-b", 2)]
    [InlineData("Name|$FN_STR_INDEXOF(\")\") * 2", "ab)", 4)]
    [InlineData("$FN_STR_COMPARE(Name, \"a+b\") - 1", "a+b", -1)]
    public void QuotedArgumentsInsideArithmetic_StayInTheOperand(string selector, string name, int expected)
    {
        AssertBothForms(selector, new TestItem { Name = name }, expected);
    }
}
