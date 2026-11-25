using Steverzag.PropertySelectors.Registration;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.Registration;

// Test-only functions with same-count overloads: no built-in function has any yet.
[SelectorToken(Token)]
public record TestPickSelector<T> : FunctionPropertySelector<T>
{
    public const string Token = "$FN_TESTSIG_PICK";
    public TestPickSelector() : base(Token) { }
    public TestPickSelector(List<PropertySelector> parameters) : base(Token, parameters) { }
    protected override IReadOnlyList<FunctionSignature> Signatures => TestPickSelector.Table;
}

[SelectorToken(TestPickSelector<object>.Token)]
public record TestPickSelector : FunctionPropertySelector
{
    internal static readonly IReadOnlyList<FunctionSignature> Table =
    [
        TestSignatures.Labelled(ArgumentConstraint.Of(typeof(int)), "int"),
        TestSignatures.Labelled(ArgumentConstraint.Numeric, "number"),
        TestSignatures.Labelled(ArgumentConstraint.String, "string"),
        TestSignatures.Labelled(ArgumentConstraint.Any, "any"),
    ];

    public TestPickSelector() : base(TestPickSelector<object>.Token) { }
    public TestPickSelector(List<PropertySelector> parameters) : base(TestPickSelector<object>.Token, parameters) { }
    protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}

[SelectorToken(Token)]
public record TestAmbiguousSelector<T> : FunctionPropertySelector<T>
{
    public const string Token = "$FN_TESTSIG_AMBIG";
    public TestAmbiguousSelector() : base(Token) { }
    public TestAmbiguousSelector(List<PropertySelector> parameters) : base(Token, parameters) { }
    protected override IReadOnlyList<FunctionSignature> Signatures => TestAmbiguousSelector.Table;
}

[SelectorToken(TestAmbiguousSelector<object>.Token)]
public record TestAmbiguousSelector : FunctionPropertySelector
{
    internal static readonly IReadOnlyList<FunctionSignature> Table =
    [
        TestSignatures.Labelled(ArgumentConstraint.Of(typeof(long)), "long"),
        TestSignatures.Labelled(ArgumentConstraint.Of(typeof(double)), "double"),
    ];

    public TestAmbiguousSelector() : base(TestAmbiguousSelector<object>.Token) { }
    public TestAmbiguousSelector(List<PropertySelector> parameters) : base(TestAmbiguousSelector<object>.Token, parameters) { }
    protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}

internal static class TestSignatures
{
    public static void Register()
    {
        SelectorRegistry.Register(TestPickSelector<object>.Token, typeof(TestPickSelector<>), typeof(TestPickSelector));
        SelectorRegistry.Register(TestAmbiguousSelector<object>.Token, typeof(TestAmbiguousSelector<>), typeof(TestAmbiguousSelector));
    }

    // The label says which overload was picked; the argument is resolved so a bad one still fails when built.
    public static FunctionSignature Labelled(ArgumentConstraint constraint, string label) =>
        new([constraint], (_, _) => typeof(string), p => target =>
        {
            _ = ExpressionUtils.ResolveArgument(target, p[0]);
            return Expression.Constant(label);
        });
}

public class SignatureResolutionTests
{
    public class Item
    {
        public int Count { get; set; }
        public int? MaybeCount { get; set; }
        public long Total { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; } = "Ann";
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public bool Flag { get; set; }
        public List<Line> Lines { get; set; } = [new() { Weight = 2, Bonus = 1 }, new() { Weight = 1, Bonus = 3 }];
    }

    public class Line
    {
        public int Weight { get; set; }
        public int Bonus { get; set; }
    }

    public SignatureResolutionTests() => TestSignatures.Register();

    private static Expression Body(PropertySelector selector) =>
        ExpressionUtils.CreateMemberAccess(Expression.Parameter(typeof(Item), "x"), selector);

    private static object? Evaluate(PropertySelector selector)
    {
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(new Item());
    }

    // ── Tie-breaking: exact beats implicit beats any ────────────────────────

    [Theory]
    [InlineData("$FN_TESTSIG_PICK(Count)", "int")]
    [InlineData("$FN_TESTSIG_PICK(MaybeCount)", "int")]
    [InlineData("$FN_TESTSIG_PICK(5)", "int")]
    [InlineData("$FN_TESTSIG_PICK(Total)", "number")]
    [InlineData("$FN_TESTSIG_PICK(Price)", "number")]
    [InlineData("$FN_TESTSIG_PICK(1.5)", "number")]
    [InlineData("$FN_TESTSIG_PICK(Name)", "string")]
    [InlineData("$FN_TESTSIG_PICK(\"x\")", "string")]
    [InlineData("$FN_TESTSIG_PICK(Created)", "any")]
    [InlineData("$FN_TESTSIG_PICK(Name|$FN_STR_LENGTH)", "int")]
    [InlineData("$FN_TESTSIG_PICK(Count * 2L)", "number")]
    [InlineData("$FN_TESTSIG_AMBIG(Total)", "long")]
    [InlineData("$FN_TESTSIG_AMBIG(1.5)", "double")]
    public void TheBestOverloadIsPicked_InBothForms(string text, string expected)
    {
        Assert.Equal(expected, Evaluate((PropertySelector<Item>)text));
        Assert.Equal(expected, Evaluate((PropertySelector)text));
    }

    [Fact]
    public void GenericForm_ResolvesWhenParsed()
    {
        var selector = (FunctionPropertySelector<Item>)(PropertySelector<Item>)"$FN_TESTSIG_PICK(Total)";

        Assert.Same(TestPickSelector.Table[1], selector.Signature);
        Assert.Equal(typeof(string), selector.Type);
    }

    [Fact]
    public void NonGenericForm_WithUntypedArguments_ResolvesWhenBuilt()
    {
        var selector = (FunctionPropertySelector)(PropertySelector)"$FN_TESTSIG_PICK(Total)";

        Assert.Null(selector.Signature);
        Assert.Null(selector.Type);
        Assert.Empty(selector.Overloads);
        Assert.Equal("number", Evaluate(selector));
    }

    [Fact]
    public void NonGenericForm_WithTypedArguments_ResolvesWhenParsed()
    {
        var selector = (FunctionPropertySelector)(PropertySelector)"$FN_TESTSIG_PICK(5)";

        Assert.Same(TestPickSelector.Table[0], selector.Signature);
    }

    [Fact]
    public void NonGenericForm_WithASingleCandidate_ResolvesWhenParsed()
    {
        var selector = (FunctionPropertySelector)(PropertySelector)"$FN_MATH_ROUND(Price, 2)";

        Assert.NotNull(selector.Signature);
        Assert.Single(selector.Overloads);
    }

    // ── Ambiguity and mismatches ────────────────────────────────────────────

    [Fact]
    public void AnAmbiguousCall_IsRejected_NamingTheCandidates()
    {
        const string text = "$FN_TESTSIG_AMBIG(Count)";
        const string message = "$FN_TESTSIG_AMBIG(Count) is ambiguous between (an Int64) and (a Double).";

        var generic = Assert.Throws<SelectorParseException>(() => (PropertySelector<Item>)text);
        Assert.Equal(message, generic.Detail);

        PropertySelector nonGeneric = text;
        var whenBuilt = Assert.Throws<InvalidOperationException>(() => Body(nonGeneric));
        Assert.Equal(message, whenBuilt.Message);
    }

    [Fact]
    public void NoMatchingOverload_ListsWhatTheFunctionTakes()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Item>)"$FN_TESTSIG_AMBIG(Flag)");

        Assert.Equal("$FN_TESTSIG_AMBIG has no overload for (Boolean): '$FN_TESTSIG_AMBIG(Flag)'. It takes (an Int64) or (a Double).", exception.Detail);
    }

    [Fact]
    public void ATargetOfTheWrongKind_IsRejected()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Item>)"Name|$FN_AGGR_SUM");

        Assert.Equal("$FN_AGGR_SUM must be applied to a collection, not a String: '$FN_AGGR_SUM'.", exception.Detail);
    }

    [Fact]
    public void APipedValueOfTheWrongKind_IsNamed()
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Item>)"Name|$FN_MATH_ROUND(2)");

        Assert.Equal("$FN_MATH_ROUND: the piped value 'Name' must be a number, not a String: 'Name|$FN_MATH_ROUND(2)'.", exception.Detail);
    }

    [Theory]
    [InlineData("$FN_STR_JOIN(\",\")", "$FN_STR_JOIN can't be called with 1 argument: '$FN_STR_JOIN(\",\")'. It takes 2 or more arguments.")]
    [InlineData("Name|$FN_COALESCE", "$FN_COALESCE can't be called with no arguments: '$FN_COALESCE'. It takes 1 or more arguments.")]
    [InlineData("$FN_STR_CONCAT(Name)", "$FN_STR_CONCAT can't be called with 1 argument: '$FN_STR_CONCAT(Name)'. It takes 2, 3 or 4 arguments.")]
    [InlineData("Name|$FN_STR_CONCAT", "$FN_STR_CONCAT can't be called with no arguments besides the piped value: 'Name|$FN_STR_CONCAT'. It takes 1, 2 or 3 arguments besides the piped value. The piped value is its first argument, so 'Name|$FN_STR_CONCAT(…)' means '$FN_STR_CONCAT(Name, …)'.")]
    public void WrongArgumentCount_SaysWhichCountsAreAccepted(string text, string expected)
    {
        var exception = Assert.Throws<SelectorParseException>(() => (PropertySelector<Item>)text);

        Assert.Equal(expected, exception.Detail);
    }

    // ── Declared type = built type for the collection and date functions ────

    [Theory]
    [InlineData("Lines.Weight|$FN_AGGR_COUNT")]
    [InlineData("Lines.Weight|$FN_AGGR_LONG_COUNT")]
    [InlineData("Lines.Weight|$FN_AGGR_COUNTDISTINCT")]
    [InlineData("Lines.Weight|$FN_AGGR_SUM")]
    [InlineData("Lines.Weight|$FN_AGGR_AVG")]
    [InlineData("Lines.Weight|$FN_AGGR_MIN")]
    [InlineData("Lines.Weight|$FN_AGGR_MAX")]
    [InlineData("Lines|$FN_ELM_FIRST")]
    [InlineData("Lines|$FN_ELM_LAST")]
    [InlineData("Lines|$FN_ELM_ATINDEX(1)")]
    [InlineData("Lines|$FN_ORDER_ASC(Weight)")]
    [InlineData("Lines|$FN_ORDER_DESC")]
    [InlineData("Lines|$FN_ORDER_ASC(Weight)|$FN_THEN_ORDER_DESC(Bonus)")]
    [InlineData("Lines|$FN_ORDER_ASC(Weight)|$FN_ELM_FIRST")]
    [InlineData("Deleted|$FN_DT_ADDDAYS(1)")]
    [InlineData("$FN_DT_ADDDAYS(Deleted, 1)")]
    [InlineData("MaybeCount|$FN_COALESCE(Count)")]
    public void DeclaredType_IsTheBuiltType(string text)
    {
        PropertySelector<Item> selector = text;

        Assert.NotNull(selector.Type);
        Assert.Equal(Body(selector).Type, selector.Type);
    }

    [Fact]
    public void BothForms_ShareOneSignatureTable()
    {
        var generic = (FunctionPropertySelector<string>)((PropertySelector<Item>)"Name|$FN_STR_SUBSTRING(1, 2)").ChildSelector!;
        var nonGeneric = (FunctionPropertySelector)((PropertySelector)"Name|$FN_STR_SUBSTRING(1, 2)").ChildSelector!;

        Assert.NotNull(generic.Signature);
        Assert.Same(generic.Signature, nonGeneric.Signature);
    }
}
