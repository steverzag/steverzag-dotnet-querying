using Steverzag.PropertySelectors.MetaData;
using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.Tests.Registration;
using System.Reflection;

namespace Steverzag.PropertySelectors.Tests.MetaData;

public class FunctionCatalogTests
{
    public class Item
    {
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int Count { get; set; }
        public string Name { get; set; } = "Ann";
        public DateTime Created { get; set; }
        public DateTimeOffset Stamp { get; set; }
        public DateOnly? Day { get; set; }
        public TimeOnly OpensAt { get; set; }
        public bool Flag { get; set; }
        public List<int> Numbers { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    public FunctionCatalogTests() => TestPipeSum.Register();

    private static readonly HashSet<string> BuiltInTokens = typeof(SelectorRegistry).Assembly.GetTypes()
        .Select(t => t.GetCustomAttribute<SelectorTokenAttribute>()?.Token)
        .OfType<string>()
        .ToHashSet();

    private static List<string> TokensAfter(Type type) =>
        FunctionCatalog.After(type).Select(m => m.Function.Token).Distinct().ToList();

    // ── All and Find ────────────────────────────────────────────────────────

    [Fact]
    public void All_ListsEveryBuiltInFunction_OrderedByToken()
    {
        var tokens = FunctionCatalog.All.Select(f => f.Token).ToList();

        Assert.Subset(tokens.ToHashSet(), BuiltInTokens);
        Assert.Equal(tokens.Order(StringComparer.Ordinal), tokens);
    }

    [Fact]
    public void EveryBuiltInFunction_HasACategory_BothForms_AndSignatures()
    {
        foreach (var function in FunctionCatalog.All.Where(f => BuiltInTokens.Contains(f.Token)))
        {
            Assert.NotEqual(FunctionCategory.Other, function.Category);
            Assert.True(function.HasGenericForm, function.Token);
            Assert.True(function.HasNonGenericForm, function.Token);
            Assert.NotEmpty(function.Signatures);
        }
    }

    [Theory]
    [InlineData("$FN_AGGR_SUM", FunctionCategory.Aggregation)]
    [InlineData("$FN_ELM_FIRST", FunctionCategory.Element)]
    [InlineData("$FN_ORDER_ASC", FunctionCategory.Ordering)]
    [InlineData("$FN_MATH_ROUND", FunctionCategory.Math)]
    [InlineData("$FN_STR_TRIM", FunctionCategory.String)]
    [InlineData("$FN_DT_ADDDAYS", FunctionCategory.Date)]
    [InlineData("$FN_DT_DATEONLY", FunctionCategory.Date)]
    [InlineData("$FN_COALESCE", FunctionCategory.Null)]
    public void Find_ReportsTheCategory(string token, FunctionCategory category)
    {
        Assert.Equal(category, FunctionCatalog.Find(token)!.Category);
    }

    [Fact]
    public void Find_ReportsTheRegistrationDetails()
    {
        var round = FunctionCatalog.Find("$FN_MATH_ROUND")!;
        Assert.Equal(PipedForm.FirstArgument, round.Piped);
        Assert.Equal(ArgumentScope.Root, round.Arguments);
        Assert.Equal(2, round.Signatures.Count);

        var join = FunctionCatalog.Find("$FN_STR_JOIN")!;
        Assert.Equal(PipedForm.Rejected, join.Piped);
        Assert.NotNull(join.PipedHint);

        var order = FunctionCatalog.Find("$FN_ORDER_ASC")!;
        Assert.Equal(PipedForm.Target, order.Piped);
        Assert.Equal(ArgumentScope.TargetElement, order.Arguments);
    }

    [Theory]
    [InlineData("$FN_NOPE")]
    [InlineData("$fn_math_round")]
    [InlineData("$FN_MATH_ROUND(2)")]
    public void Find_ReturnsNull_ForAnUnknownToken(string token)
    {
        Assert.Null(FunctionCatalog.Find(token));
    }

    [Fact]
    public void AnExternalFunction_IsListed_WithTheDefaultCategory()
    {
        var function = FunctionCatalog.Find(TestPipeSumSelector<object>.Token)!;

        Assert.Equal(FunctionCategory.Other, function.Category);
        Assert.Equal(PipedForm.FirstArgument, function.Piped);
        Assert.Same(TestPipeSum.Table, function.Signatures);

        var match = Assert.Single(FunctionCatalog.After(typeof(int)), m => m.Function.Token == TestPipeSumSelector<object>.Token);
        Assert.Equal(typeof(int), match.ResultType);
    }

    // ── After ───────────────────────────────────────────────────────────────

    [Fact]
    public void After_ANumber_ListsEachOverload_WithoutThePipedArgument()
    {
        var rounds = FunctionCatalog.After(typeof(decimal)).Where(m => m.Function.Token == "$FN_MATH_ROUND").ToList();

        Assert.Equal(2, rounds.Count);
        Assert.Empty(rounds[0].Parameters);
        Assert.Same(ArgumentConstraint.Integer, Assert.Single(rounds[1].Parameters));
        Assert.All(rounds, m => Assert.Equal(typeof(decimal), m.ResultType));
    }

    [Fact]
    public void After_AResultThatDependsOnAnArgument_IsUnknown()
    {
        var max = Assert.Single(FunctionCatalog.After(typeof(decimal)), m => m.Function.Token == "$FN_MATH_MAX");

        Assert.Same(ArgumentConstraint.Numeric, Assert.Single(max.Parameters));
        Assert.Null(max.ResultType);
    }

    [Fact]
    public void After_ANumber_ListsOnlyMathFunctions()
    {
        Assert.All(FunctionCatalog.After(typeof(decimal)).Where(m => BuiltInTokens.Contains(m.Function.Token)),
            m => Assert.Equal(FunctionCategory.Math, m.Function.Category));
    }

    [Fact]
    public void After_ANullableValue_OffersCoalesce_AndANonNullableOneDoesNot()
    {
        Assert.Contains("$FN_COALESCE", TokensAfter(typeof(decimal?)));
        Assert.Contains("$FN_COALESCE", TokensAfter(typeof(string)));
        Assert.DoesNotContain("$FN_COALESCE", TokensAfter(typeof(decimal)));
    }

    [Fact]
    public void After_AString_IsNotTreatedAsACollection()
    {
        var tokens = TokensAfter(typeof(string));

        Assert.Contains("$FN_STR_TRIM", tokens);
        Assert.DoesNotContain(tokens, t => t.StartsWith("$FN_AGGR_") || t.StartsWith("$FN_ELM_") || t.StartsWith("$FN_ORDER_"));
    }

    [Fact]
    public void After_ACollection_ListsAggregatesElementsAndOrdering_ButNotThenOrder()
    {
        var matches = FunctionCatalog.After(typeof(List<int>));
        var tokens = matches.Select(m => m.Function.Token).ToList();

        Assert.Contains("$FN_AGGR_AVG", tokens);
        Assert.Contains("$FN_ELM_FIRST", tokens);
        Assert.Contains("$FN_ORDER_ASC", tokens);
        Assert.DoesNotContain("$FN_THEN_ORDER_ASC", tokens);
        Assert.Equal(typeof(double?), matches.First(m => m.Function.Token == "$FN_AGGR_AVG").ResultType);
        Assert.Equal(typeof(IOrderedEnumerable<int>), matches.First(m => m.Function.Token == "$FN_ORDER_ASC").ResultType);
    }

    [Fact]
    public void After_ACollectionOfStrings_LeavesOutTheNumericAggregates()
    {
        var tokens = TokensAfter(typeof(List<string>));

        Assert.Contains("$FN_AGGR_MAX", tokens);
        Assert.DoesNotContain("$FN_AGGR_SUM", tokens);
        Assert.DoesNotContain("$FN_AGGR_AVG", tokens);
    }

    [Fact]
    public void After_AnOrderedCollection_ListsThenOrder()
    {
        Assert.Contains("$FN_THEN_ORDER_ASC", TokensAfter(typeof(IOrderedEnumerable<int>)));
        Assert.Contains("$FN_THEN_ORDER_DESC", TokensAfter(typeof(IOrderedQueryable<int>)));
    }

    [Theory]
    [InlineData(typeof(DateTime), new[] { "$FN_DT_ADDYEARS", "$FN_DT_ADDDAYS", "$FN_DT_ADDHOURS", "$FN_DT_ADDSECONDS", "$FN_DT_ADDMILLISECONDS" }, new string[0])]
    [InlineData(typeof(DateOnly), new[] { "$FN_DT_ADDYEARS", "$FN_DT_ADDMONTHS", "$FN_DT_ADDDAYS" }, new[] { "$FN_DT_ADDHOURS", "$FN_DT_ADDMINUTES", "$FN_DT_ADDSECONDS" })]
    [InlineData(typeof(TimeOnly), new[] { "$FN_DT_ADDHOURS", "$FN_DT_ADDMINUTES" }, new[] { "$FN_DT_ADDDAYS", "$FN_DT_ADDSECONDS", "$FN_DT_ADDMILLISECONDS" })]
    public void After_ADateType_ListsOnlyTheAddsItHas(Type type, string[] offered, string[] missing)
    {
        var tokens = TokensAfter(type);

        Assert.Subset(tokens.ToHashSet(), offered.ToHashSet());
        Assert.All(missing, t => Assert.DoesNotContain(t, tokens));
    }

    [Theory]
    [InlineData(typeof(bool))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(Guid))]
    public void After_ATypeNoFunctionTakes_IsEmpty(Type type)
    {
        Assert.DoesNotContain(FunctionCatalog.After(type), m => BuiltInTokens.Contains(m.Function.Token));
    }

    [Fact]
    public void After_NeverListsAFunctionThatRejectsThePipe()
    {
        foreach (var type in new[] { typeof(string), typeof(DateTime), typeof(int), typeof(object) })
            Assert.DoesNotContain(FunctionCatalog.After(type), m => m.Function.Piped == PipedForm.Rejected);
    }

    [Fact]
    public void After_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FunctionCatalog.After(null!));
    }

    // ── Every match parses ──────────────────────────────────────────────────

    public static TheoryData<string> Subjects() =>
    [
        nameof(Item.Price), nameof(Item.Discount), nameof(Item.Count), nameof(Item.Name), nameof(Item.Created),
        nameof(Item.Stamp), nameof(Item.Day), nameof(Item.OpensAt), nameof(Item.Flag), nameof(Item.Numbers),
        nameof(Item.Tags), "Numbers|$FN_ORDER_ASC",
    ];

    private static string? SampleArgument(ArgumentConstraint constraint)
    {
        if (constraint == ArgumentConstraint.Numeric || constraint == ArgumentConstraint.Integer || constraint == ArgumentConstraint.IntegerLiteral)
            return "1";
        if (constraint == ArgumentConstraint.String || constraint == ArgumentConstraint.StringLiteral)
            return "\"x\"";
        return null;
    }

    [Theory]
    [MemberData(nameof(Subjects))]
    public void EveryMatch_Parses_AndDeclaresThePredictedType(string subject)
    {
        var subjectType = ((PropertySelector<Item>)subject).Type!;
        var matches = FunctionCatalog.After(subjectType).Where(m => BuiltInTokens.Contains(m.Function.Token)).ToList();
        if (subjectType != typeof(bool))
            Assert.NotEmpty(matches);

        foreach (var match in matches)
        {
            var arguments = match.Parameters.Select(SampleArgument).ToList();
            if (arguments.Contains(null))
                continue;

            var call = arguments.Count == 0 ? match.Function.Token : $"{match.Function.Token}({string.Join(", ", arguments)})";
            var text = $"{subject}|{call}";

            var selector = (PropertySelector<Item>)text;
            if (match.ResultType is { } expected)
                Assert.True(expected == selector.Type, $"{text}: the catalog says {expected}, parsing declares {selector.Type}.");
        }
    }
}
