using Steverzag.PropertySelectors.Registration;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.Registration;

[SelectorToken(Token, Piped = PipedForm.FirstArgument)]
public record TestPipeSumSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
    public const string Token = "$FN_TESTPIPE_SUM";

    public TestPipeSumSelector() : base(Token) { }
    public TestPipeSumSelector(List<PropertySelector> parameters) : base(Token, parameters) { }
    public static IReadOnlyList<FunctionSignature> Signatures => TestPipeSum.Table;
}

[SelectorToken(TestPipeSumSelector<object>.Token, Piped = PipedForm.FirstArgument)]
public record TestPipeSumSelector : FunctionPropertySelector, IFunctionSignatures
{
    public TestPipeSumSelector() : base(TestPipeSumSelector<object>.Token) { }
    public TestPipeSumSelector(List<PropertySelector> parameters) : base(TestPipeSumSelector<object>.Token, parameters) { }
    public static IReadOnlyList<FunctionSignature> Signatures => TestPipeSum.Table;
}

internal static class TestPipeSum
{
    public static void Register() =>
        SelectorRegistry.Register(TestPipeSumSelector<object>.Token, typeof(TestPipeSumSelector<>), typeof(TestPipeSumSelector));

    public static readonly IReadOnlyList<FunctionSignature> Table =
    [
        new([ArgumentConstraint.Any, ArgumentConstraint.Any], (_, _) => typeof(int), p => target => Expression.Add(
            ExpressionUtils.ResolveArgument(target, p[0]),
            ExpressionUtils.ResolveArgument(target, p[1]))),
    ];
}

public class PipedSubjectTests
{
    public class TestItem
    {
        public int A { get; set; }
        public int B { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public InnerItem Inner { get; set; } = new();
    }

    public class InnerItem
    {
        public int A { get; set; }
        public int B { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Created { get; set; }
    }

    private static readonly TestItem Item = new()
    {
        A = 2,
        B = 30,
        Name = "xyz",
        Created = new DateTime(2024, 1, 1),
        Inner = new() { A = 400, B = 5000, Name = "inner", Created = new DateTime(1999, 1, 1) },
    };

    public PipedSubjectTests() => TestPipeSum.Register();

    public static TheoryData<string, int> Evaluations => new()
    {
        { "$FN_TESTPIPE_SUM(A, B)", 32 },
        { "A|$FN_TESTPIPE_SUM(5)", 7 },
        { "Inner.A|$FN_TESTPIPE_SUM(1)", 401 },
        { "A|$FN_TESTPIPE_SUM(5)|$FN_TESTPIPE_SUM(10)", 17 },
        { "Inner.A|$FN_TESTPIPE_SUM(B)", 430 },
        { "A|$FN_TESTPIPE_SUM(Inner.A)", 402 },
        { "Inner.A|$FN_TESTPIPE_SUM(B)|$FN_TESTPIPE_SUM(A)", 432 },
        { "$FN_TESTPIPE_SUM(5, A)|$FN_TESTPIPE_SUM(B)", 37 },
    };

    [Fact]
    public void PipedForm_WithTheSubjectAlsoInTheArguments_IsRejected()
    {
        Assert.Throws<SelectorParseException>(() => (PropertySelector)"Inner|$FN_TESTPIPE_SUM(A, B)");
        Assert.Throws<SelectorParseException>(() => (PropertySelector<TestItem>)"Inner|$FN_TESTPIPE_SUM(A, B)");
    }

    [Fact]
    public void PipedBuiltInFunction_ResolvesItsOtherArgumentsAgainstTheRoot()
    {
        foreach (var selector in BothForms("Inner.A|$FN_MATH_MAX(B)"))
            Assert.Equal(400, Evaluate(selector));

        foreach (var selector in BothForms("Inner.Name|$FN_STR_CONCAT(Name)"))
            Assert.Equal("innerxyz", Evaluate<string>(selector));

        foreach (var selector in BothForms("Inner.Created|$FN_DT_ADDDAYS(B)"))
            Assert.Equal(new DateTime(1999, 1, 31), Evaluate<DateTime>(selector));
    }

    [Fact]
    public void PipedBuiltInFunction_WithTheSubjectAlsoInTheArguments_IsRejected()
    {
        Assert.Throws<SelectorParseException>(() => (PropertySelector)"Inner|$FN_MATH_MAX(A, 1)");
        Assert.Throws<SelectorParseException>(() => (PropertySelector<TestItem>)"Inner|$FN_MATH_MAX(A, 1)");
    }

    [Theory]
    [MemberData(nameof(Evaluations))]
    public void BothForms_Evaluate(string text, int expected)
    {
        foreach (var selector in BothForms(text))
            Assert.Equal(expected, Evaluate(selector));
    }

    [Fact]
    public void PipedForm_BuildsTheSameExpressionAsTheStaticForm()
    {
        foreach (var (piped, @static) in BothForms("A|$FN_TESTPIPE_SUM(5)").Zip(BothForms("$FN_TESTPIPE_SUM(A, 5)")))
            Assert.Equal(Body(@static).ToString(), Body(piped).ToString());
    }

    [Fact]
    public void PipedForm_GetsThePlaceholderAsArgumentZero()
    {
        foreach (var selector in BothForms("A|$FN_TESTPIPE_SUM(5)"))
        {
            var arguments = Parameters(selector.ChildSelector!);

            Assert.Equal(2, arguments.Count);
            Assert.IsType<PipedValueSelector>(arguments[0]);
        }
    }

    [Fact]
    public void StaticForm_GetsNoPlaceholder()
    {
        foreach (var selector in BothForms("$FN_TESTPIPE_SUM(A, B)"))
            Assert.DoesNotContain(Parameters(selector), p => p is PipedValueSelector);
    }

    [Fact]
    public void Placeholder_CarriesThePipedTypeInTheGenericForm()
    {
        Assert.Equal(typeof(int), Parameters(((PropertySelector<TestItem>)"A|$FN_TESTPIPE_SUM(5)").ChildSelector!)[0].Type);
        Assert.Null(Parameters(((PropertySelector)"A|$FN_TESTPIPE_SUM(5)").ChildSelector!)[0].Type);
    }

    [Fact]
    public void UnmarkedFunction_GetsNoPlaceholderWhenPiped()
    {
        foreach (var selector in BothForms("Name|$FN_STR_CONTAINS(\"x\")"))
        {
            Assert.Single(Parameters(selector.ChildSelector!));
            Assert.True(Evaluate<bool>(selector));
        }
    }

    [Theory]
    [InlineData("A|$FN_TESTPIPE_SUM(5)")]
    [InlineData("Inner.A|$FN_TESTPIPE_SUM(1)|$FN_TESTPIPE_SUM(B)")]
    public void ToString_WritesThePipedForm(string text)
    {
        foreach (var selector in BothForms(text))
            Assert.Equal(text, selector.ToString());
    }

    private static PropertySelector[] BothForms(string text) =>
        [(PropertySelector)text, (PropertySelector<TestItem>)text];

    private static Expression Body(PropertySelector selector) =>
        ExpressionUtils.CreateMemberAccess(Expression.Parameter(typeof(TestItem), "p"), selector);

    private static int Evaluate(PropertySelector selector) => Evaluate<int>(selector);

    private static TResult Evaluate<TResult>(PropertySelector selector)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "p");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        return Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile()(Item);
    }

    // Generic chain segments are closed over the piped type, not TestItem, so read Parameters by reflection.
    private static IReadOnlyList<PropertySelector> Parameters(PropertySelector function) =>
        (IReadOnlyList<PropertySelector>)function.GetType().GetProperty(nameof(FunctionPropertySelector.Parameters))!.GetValue(function)!;
}
