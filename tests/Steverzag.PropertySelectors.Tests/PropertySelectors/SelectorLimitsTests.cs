using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class SelectorLimitsTests
{
    public class Node
    {
        public int Age { get; set; }
        public string Name { get; set; } = string.Empty;
        public Node? Next { get; set; }
    }

    private static PropertySelector Parse(string text, bool generic)
        => generic ? (PropertySelector<Node>)text : (PropertySelector)text;

    private static SelectorParseException Rejects(string text, bool generic)
        => Assert.Throws<SelectorParseException>(() => Parse(text, generic));

    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));

    private static string NestedSqrt(int count) => Repeat("$FN_MATH_SQRT(", count) + "Age" + Repeat(")", count);

    private static string FlatSum(int terms) => string.Join(" + ", Enumerable.Repeat("Age", terms));

    private static string Chain(int trims) => "Name" + Repeat("|$FN_STR_TRIM", trims);

    private static string Path(int segments) => string.Join(".", Enumerable.Repeat("Next", segments - 1).Append("Age"));

    private static string Negations(int count) => Repeat("-", count) + "Age";

    private static string Parenthesized(int count) => Repeat("(", count) + "Age" + Repeat(")", count);

    private static void WithLimits(int maxLength, int maxDepth, Action action)
    {
        SelectorLimits.MaxLength = maxLength;
        SelectorLimits.MaxDepth = maxDepth;
        try
        {
            action();
        }
        finally
        {
            SelectorLimits.MaxLength = SelectorLimits.DefaultMaxLength;
            SelectorLimits.MaxDepth = SelectorLimits.DefaultMaxDepth;
        }
    }

    [Fact]
    public void Defaults()
    {
        Assert.Equal(2000, SelectorLimits.DefaultMaxLength);
        Assert.Equal(32, SelectorLimits.DefaultMaxDepth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Limits_MustBePositive(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectorLimits.MaxLength = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectorLimits.MaxDepth = value);
    }

    [Fact]
    public void MaxDepth_CannotExceedTheCeiling()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectorLimits.MaxDepth = SelectorLimits.MaxDepthCeiling + 1);
        Assert.Equal(SelectorLimits.DefaultMaxDepth, SelectorLimits.MaxDepth);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASelectorAtTheMaximumLength_Parses(bool generic)
    {
        var text = new string(' ', SelectorLimits.DefaultMaxLength - 3) + "Age";

        var body = ExpressionUtils.CreateMemberAccess(Expression.Parameter(typeof(Node), "x"), Parse(text, generic));

        Assert.Equal("x.Age", body.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASelectorOverTheMaximumLength_IsRejectedBeforeItIsScanned(bool generic)
    {
        var text = new string('(', SelectorLimits.DefaultMaxLength + 1);

        var exception = Rejects(text, generic);

        Assert.Equal("The selector is 2001 characters long; the limit is 2000 (SelectorLimits.MaxLength).", exception.Detail);
        Assert.Null(exception.Position);
        Assert.Equal(exception.Detail, exception.Message);
    }

    public static TheoryData<string, string, bool> AtTheMaximumDepth => new()
    {
        { "nested functions", NestedSqrt(31), true },
        { "nested functions", NestedSqrt(31), false },
        { "flat sum", FlatSum(32), true },
        { "flat sum", FlatSum(32), false },
        { "chain", Chain(31), true },
        { "chain", Chain(31), false },
        { "dotted path", Path(32), true },
        { "dotted path", Path(32), false },
        { "negations", Negations(31), true },
        { "negations", Negations(31), false },
        { "negation and function", "-" + NestedSqrt(30), true },
        { "negation and function", "-" + NestedSqrt(30), false },
    };

    [Theory]
    [MemberData(nameof(AtTheMaximumDepth))]
    public void ASelectorAtTheMaximumDepth_Parses(string shape, string text, bool generic)
    {
        var selector = Parse(text, generic);

        Assert.Equal(SelectorLimits.DefaultMaxDepth, SelectorLimits.Depth(selector));
        Assert.NotNull(shape);
    }

    public static TheoryData<string, string, bool> OverTheMaximumDepth => new()
    {
        { "nested functions", NestedSqrt(32), true },
        { "nested functions", NestedSqrt(32), false },
        { "flat sum", FlatSum(33), true },
        { "flat sum", FlatSum(33), false },
        { "chain", Chain(32), true },
        { "chain", Chain(32), false },
        { "dotted path", Path(33), true },
        { "dotted path", Path(33), false },
        { "negations", Negations(32), true },
        { "negations", Negations(32), false },
        { "negation and function", "-" + NestedSqrt(31), true },
        { "negation and function", "-" + NestedSqrt(31), false },
    };

    [Theory]
    [MemberData(nameof(OverTheMaximumDepth))]
    public void ASelectorOverTheMaximumDepth_IsRejected(string shape, string text, bool generic)
    {
        var exception = Rejects(text, generic);

        Assert.Equal("The selector is nested more than 32 levels deep (SelectorLimits.MaxDepth).", exception.Detail);
        Assert.Equal(text, exception.Selector);
        Assert.Null(exception.Position);
        Assert.NotNull(shape);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parentheses_CountTowardTheLimit(bool generic)
    {
        Assert.Equal(1, SelectorLimits.Depth(Parse(Parenthesized(31), generic)));

        var exception = Rejects(Parenthesized(32), generic);

        Assert.Equal("The selector is nested more than 32 levels deep (SelectorLimits.MaxDepth).", exception.Detail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheLimitCanBeLowered(bool generic)
    {
        WithLimits(SelectorLimits.DefaultMaxLength, 4, () =>
        {
            Parse("$FN_MATH_MAX(Age, Next.Next.Age)", generic);
            Assert.Equal(
                "The selector is nested more than 4 levels deep (SelectorLimits.MaxDepth).",
                Rejects("$FN_MATH_MAX(Age, Next.Next.Next.Age)", generic).Detail);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DepthCountsTheDeepestOperand(bool generic)
    {
        Assert.Equal(4, SelectorLimits.Depth(Parse("$FN_MATH_MAX(Age, Next.Next.Age)", generic)));
        Assert.Equal(3, SelectorLimits.Depth(Parse("Age|$FN_MATH_MAX(Next.Age)", generic)));
        Assert.Equal(3, SelectorLimits.Depth(Parse("(Age + 1)|$FN_MATH_ROUND", generic)));
    }

    public static TheoryData<string, string, bool> SelectorsThatCrashedTheProcess => new()
    {
        { "2,000 nested parentheses", Repeat("(", 2000) + "Age" + Repeat(")", 2000), true },
        { "2,000 nested parentheses", Repeat("(", 2000) + "Age" + Repeat(")", 2000), false },
        { "1,000 nested functions", NestedSqrt(1000), true },
        { "1,000 nested functions", NestedSqrt(1000), false },
        { "a flat sum of 3,000 terms", FlatSum(3000), true },
        { "a flat sum of 3,000 terms", FlatSum(3000), false },
        { "20,000 negations", Negations(20000), true },
        { "20,000 negations", Negations(20000), false },
    };

    [Theory]
    [MemberData(nameof(SelectorsThatCrashedTheProcess))]
    public void SelectorsThatCrashedTheProcess_AreRejected(string shape, string text, bool generic)
    {
        Rejects(text, generic);
        Assert.NotNull(shape);
    }

    public static TheoryData<string, string, bool> TooDeepForTheStack => new()
    {
        { "100,000 nested parentheses", Repeat("(", 100_000) + "Age" + Repeat(")", 100_000), true },
        { "100,000 nested parentheses", Repeat("(", 100_000) + "Age" + Repeat(")", 100_000), false },
        { "20,000 nested functions", NestedSqrt(20_000), true },
        { "20,000 nested functions", NestedSqrt(20_000), false },
        { "a flat sum of 20,000 terms", FlatSum(20_000), true },
        { "a flat sum of 20,000 terms", FlatSum(20_000), false },
        { "100,000 negations", Negations(100_000), true },
        { "100,000 negations", Negations(100_000), false },
    };

    [Theory]
    [MemberData(nameof(TooDeepForTheStack))]
    public void WithTheLengthLimitRaised_DeepSelectorsAreStillRejected(string shape, string text, bool generic)
    {
        WithLimits(int.MaxValue, SelectorLimits.DefaultMaxDepth, () => Rejects(text, generic));
        Assert.NotNull(shape);
    }

    [Theory]
    [MemberData(nameof(TooDeepForTheStack))]
    public void WithBothLimitsRaised_TheStackGuardRejectsWhatWouldOverflow(string shape, string text, bool generic)
    {
        WithLimits(int.MaxValue, SelectorLimits.MaxDepthCeiling, () =>
        {
            try
            {
                Parse(text, generic);
            }
            catch (SelectorParseException)
            {
            }
        });
        Assert.NotNull(shape);
    }
}
