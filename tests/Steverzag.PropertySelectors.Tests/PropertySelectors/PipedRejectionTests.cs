using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class PipedRejectionTests
{
    public class Item
    {
        public string Name { get; set; } = "Ann";
        public DateTime Created { get; set; } = new(2000, 1, 1);
        public InnerItem Inner { get; set; } = new();
    }

    public class InnerItem
    {
        public int A { get; set; }
    }

    public static TheoryData<string, string> RejectedFunctions => new()
    {
        { "Created", "$FN_DT_NOW" },
        { "Created", "$FN_DT_UTCNOW" },
        { "Created", "$FN_DT_TODAY" },
        { "Name", "$FN_DT_DATETIME(\"2024-01-01T08:30:00\")" },
        { "Name", "$FN_DT_DATEONLY(\"2024-01-01\")" },
        { "Name", "$FN_DT_TIMEONLY(\"08:30\")" },
        { "Name", "$FN_STR_JOIN(\",\", \"x\")" },
        { "Name|$FN_STR_TRIM", "$FN_DT_NOW" },
    };

    [Theory]
    [MemberData(nameof(RejectedFunctions))]
    public void FunctionWithoutASubject_RejectsThePipedForm(string piped, string function)
    {
        var text = $"{piped}|{function}";
        var token = function.Split('(')[0];
        var expected = $"{token} takes no piped value, but '{piped}' is piped into it. Write it at the start of a chain: '{function}'.";

        foreach (var parse in BothParsers)
        {
            var exception = Assert.Throws<ArgumentException>(() => parse(text));
            Assert.StartsWith(expected, exception.Message);
        }
    }

    [Theory]
    [MemberData(nameof(RejectedFunctions))]
    public void FunctionWithoutASubject_StillWorksAtTheStartOfAChain(string _, string function)
    {
        foreach (var parse in BothParsers)
            Assert.NotNull(Evaluate(parse(function)));
    }

    [Fact]
    public void StringJoin_ExplainsWhereThePipedValueGoes()
    {
        foreach (var parse in BothParsers)
        {
            var exception = Assert.Throws<ArgumentException>(() => parse("Name|$FN_STR_JOIN(\",\", \"x\")"));
            Assert.EndsWith(
                "Its first argument is the separator. To join the piped value, pass it as one of the values after the separator.",
                exception.Message);
        }
    }

    [Fact]
    public void PipedForm_WithTheSubjectRepeated_ExplainsThePipedRule()
    {
        foreach (var parse in BothParsers)
        {
            var exception = Assert.Throws<ArgumentException>(() => parse("Inner|$FN_MATH_MAX(A, 1)"));
            Assert.Equal(
                "$FN_MATH_MAX can't be called with 2 arguments besides the piped value: 'Inner|$FN_MATH_MAX(A, 1)'. " +
                "It takes 1 argument besides the piped value. " +
                "The piped value is its first argument, so 'Inner|$FN_MATH_MAX(…)' means '$FN_MATH_MAX(Inner, …)'.",
                exception.Message);
        }
    }

    [Theory]
    [InlineData("$FN_MATH_MAX(1)", "$FN_MATH_MAX can't be called with 1 argument: '$FN_MATH_MAX(1)'. It takes 2 arguments.")]
    [InlineData("$FN_MATH_ROUND", "$FN_MATH_ROUND can't be called with no arguments: '$FN_MATH_ROUND'. It takes 1 or 2 arguments.")]
    [InlineData("$FN_MATH_ROUND(1, 2, 3)", "$FN_MATH_ROUND can't be called with 3 arguments: '$FN_MATH_ROUND(1, 2, 3)'. It takes 1 or 2 arguments.")]
    [InlineData("Name|$FN_STR_TRIM(1)", "$FN_STR_TRIM can't be called with 1 argument: '$FN_STR_TRIM(1)'. It takes no arguments.")]
    public void WrongArgumentCount_SaysWhatWasWritten(string text, string expected)
    {
        foreach (var parse in BothParsers)
            Assert.Equal(expected, Assert.Throws<ArgumentException>(() => parse(text)).Message);
    }

    private static readonly Func<string, PropertySelector>[] BothParsers =
    [
        text => (PropertySelector)text,
        text => (PropertySelector<Item>)text,
    ];

    private static object? Evaluate(PropertySelector selector)
    {
        var root = Expression.Parameter(typeof(Item), "p");
        var body = ExpressionUtils.CreateMemberAccess(root, selector);
        return Expression.Lambda<Func<Item, object?>>(Expression.Convert(body, typeof(object)), root).Compile()(new Item());
    }
}
