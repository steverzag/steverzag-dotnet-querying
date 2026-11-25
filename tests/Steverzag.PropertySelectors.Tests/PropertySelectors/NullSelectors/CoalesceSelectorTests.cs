using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.NullSelectors;

public class CoalesceSelectorTests
{
    private class TestItem
    {
        public int Quantity { get; set; }
        public int? First { get; set; }
        public int? Second { get; set; }
        public int? Third { get; set; }
        public long LongValue { get; set; }
        public decimal? Price { get; set; }
        public string? Nickname { get; set; }
        public string Name { get; set; } = "name";
        public DateTime Created { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DayOfWeek? Day { get; set; }
        public List<int> Numbers { get; set; } = [];
        public List<int?> NullableNumbers { get; set; } = [];
    }

    private const string Coalesce = SelectorDefaults.Coalesce;
    private const string Max = SelectorDefaults.MaxAggregateFunction;

    private static readonly DateTime CreatedAt = new(2024, 1, 1);
    private static readonly DateTime UpdatedAt = new(2024, 6, 1);

    private static Expression Build(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(TestItem), "x");
        return ExpressionUtils.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate(PropertySelector selector, TestItem item)
    {
        var body = Build(selector, out var parameter);
        return Expression.Lambda<Func<TestItem, object?>>(Expression.Convert(body, typeof(object)), parameter).Compile()(item);
    }

    private static Type BuiltType(PropertySelector selector) => Build(selector, out _).Type;

    public static TheoryData<bool> BothForms() => new() { true, false };

    private static PropertySelector Parse(string selector, bool generic)
        => generic ? (PropertySelector<TestItem>)selector : (PropertySelector)selector;

    // ── Aggregates over empty collections (the decision 014 follow-up) ─────

    [Theory]
    [MemberData(nameof(BothForms))]
    public void AfterMax_OnEmptyCollection_ReturnsTheDefault(bool generic)
    {
        var selector = Parse($"Numbers|{Max}|{Coalesce}(0)", generic);

        Assert.Equal(0, Evaluate(selector, new TestItem { Numbers = [] }));
        Assert.Equal(typeof(int), BuiltType(selector));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void AfterMax_OnNonEmptyCollection_ReturnsTheMax(bool generic)
    {
        var selector = Parse($"Numbers|{Max}|{Coalesce}(0)", generic);

        Assert.Equal(7, Evaluate(selector, new TestItem { Numbers = [3, 7, 5] }));
    }

    [Fact]
    public void AfterMax_DeclaresTheNonNullableType()
    {
        PropertySelector<TestItem> selector = $"Numbers|{Max}|{Coalesce}(0)";

        Assert.Equal(typeof(int), selector.Type);
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void AfterMax_DefaultCanBeAProperty(bool generic)
    {
        var selector = Parse($"Numbers|{Max}|{Coalesce}(Quantity)", generic);

        Assert.Equal(9, Evaluate(selector, new TestItem { Numbers = [], Quantity = 9 }));
        Assert.Equal(3, Evaluate(selector, new TestItem { Numbers = [3], Quantity = 9 }));
    }

    // ── Nullable properties ─────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NullableProperty_WithLiteralDefault(bool generic)
    {
        var selector = Parse($"First|{Coalesce}(0)", generic);

        Assert.Equal(0, Evaluate(selector, new TestItem { First = null }));
        Assert.Equal(4, Evaluate(selector, new TestItem { First = 4 }));
        Assert.Equal(typeof(int), BuiltType(selector));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NullableString_WithLiteralDefault(bool generic)
    {
        var selector = Parse($"Nickname|{Coalesce}(\"n/a\")", generic);

        Assert.Equal("n/a", Evaluate(selector, new TestItem { Nickname = null }));
        Assert.Equal("Bob", Evaluate(selector, new TestItem { Nickname = "Bob" }));
        Assert.Equal(typeof(string), BuiltType(selector));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NullableDate_FallsBackToAnotherProperty(bool generic)
    {
        var selector = Parse($"LastUpdated|{Coalesce}(Created)", generic);

        Assert.Equal(CreatedAt, Evaluate(selector, new TestItem { Created = CreatedAt }));
        Assert.Equal(UpdatedAt, Evaluate(selector, new TestItem { Created = CreatedAt, LastUpdated = UpdatedAt }));
        Assert.Equal(typeof(DateTime), BuiltType(selector));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NullableDate_WithTypedLiteralDefault(bool generic)
    {
        var selector = Parse($"LastUpdated|{Coalesce}({SelectorDefaults.DateTimeLiteral}(\"2024-01-01\"))", generic);

        Assert.Equal(CreatedAt, Evaluate(selector, new TestItem()));
        Assert.Equal(typeof(DateTime), BuiltType(selector));
    }

    [Fact]
    public void PropertyFallback_DeclaresTheBuiltType_InTheGenericForm()
    {
        PropertySelector<TestItem> selector = $"LastUpdated|{Coalesce}(Created)";

        Assert.Equal(typeof(DateTime), selector.Type);
        Assert.Equal(BuiltType(selector), selector.Type);
        Assert.Null(((PropertySelector)$"LastUpdated|{Coalesce}(Created)").Type);
    }

    // ── Several operands ───────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(BothForms))]
    public void SeveralOperands_ReturnTheFirstNonNull(bool generic)
    {
        var selector = Parse($"First|{Coalesce}(Second, Third, 0)", generic);

        Assert.Equal(1, Evaluate(selector, new TestItem { First = 1, Second = 2, Third = 3 }));
        Assert.Equal(2, Evaluate(selector, new TestItem { Second = 2, Third = 3 }));
        Assert.Equal(3, Evaluate(selector, new TestItem { Third = 3 }));
        Assert.Equal(0, Evaluate(selector, new TestItem()));
        Assert.Equal(typeof(int), BuiltType(selector));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NullableLastOperand_KeepsTheResultNullable(bool generic)
    {
        var selector = Parse($"First|{Coalesce}(Second)", generic);

        Assert.Null(Evaluate(selector, new TestItem()));
        Assert.Equal(2, Evaluate(selector, new TestItem { Second = 2 }));
        Assert.Equal(typeof(int?), BuiltType(selector));
    }

    // ── Numbers widen ──────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(BothForms))]
    public void Numbers_WidenToTheLargerType(bool generic)
    {
        var selector = Parse($"First|{Coalesce}(LongValue)", generic);

        Assert.Equal(5_000_000_000L, Evaluate(selector, new TestItem { LongValue = 5_000_000_000L }));
        Assert.Equal(2L, Evaluate(selector, new TestItem { First = 2, LongValue = 5 }));
        Assert.Equal(typeof(long), BuiltType(selector));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void IntegerLiteral_WidensToDecimal(bool generic)
    {
        var selector = Parse($"Price|{Coalesce}(0)", generic);

        Assert.Equal(0m, Evaluate(selector, new TestItem()));
        Assert.Equal(2.5m, Evaluate(selector, new TestItem { Price = 2.5m }));
        Assert.Equal(typeof(decimal), BuiltType(selector));
    }

    [Fact]
    public void IntegerLiteral_WidensToDecimal_Declared()
    {
        PropertySelector<TestItem> selector = $"Price|{Coalesce}(0)";

        Assert.Equal(typeof(decimal), selector.Type);
    }

    // ── Composition ────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(BothForms))]
    public void InArithmetic(bool generic)
    {
        var selector = Parse($"First|{Coalesce}(0) + 1", generic);

        Assert.Equal(1, Evaluate(selector, new TestItem()));
        Assert.Equal(5, Evaluate(selector, new TestItem { First = 4 }));
    }

    [Fact]
    public void StringSubject_WithPropertyFallback_DeclaresString()
    {
        PropertySelector<TestItem> selector = $"Nickname|{Coalesce}(Name)";

        Assert.Equal(typeof(string), selector.Type);
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void ChainsIntoAnotherFunction(bool generic)
    {
        var selector = Parse($"Nickname|{Coalesce}(Name)|{SelectorDefaults.StringLength}", generic);

        Assert.Equal(4, Evaluate(selector, new TestItem { Name = "name" }));
        Assert.Equal(2, Evaluate(selector, new TestItem { Nickname = "Al" }));
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void AsAnArgumentOfAnotherFunction(bool generic)
    {
        var selector = Parse($"{SelectorDefaults.StringConcat}(Nickname|{Coalesce}(\"none\"), Name)", generic);

        Assert.Equal("nonename", Evaluate(selector, new TestItem()));
        Assert.Equal("Alname", Evaluate(selector, new TestItem { Nickname = "Al" }));
    }

    // ── Rejections ─────────────────────────────────────────────────────────

    [Fact]
    public void NonNullablePipedValue_IsRejectedWhenParsed()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => (PropertySelector<TestItem>)$"Quantity|{Coalesce}(0)");
        Assert.Contains("the piped value", ex.Message);
    }

    [Fact]
    public void NonNullablePipedValue_NonGeneric_IsRejectedWhenBuilt()
    {
        PropertySelector selector = $"Quantity|{Coalesce}(0)";

        var ex = Assert.Throws<InvalidOperationException>(() => Build(selector, out _));
        Assert.Contains("the piped value", ex.Message);
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NonNullableOperandBeforeTheLast_IsRejected(bool generic)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(Parse($"First|{Coalesce}(0, Second)", generic), out _));
        Assert.Contains("argument 1", ex.Message);
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void NonNullablePropertyBeforeTheLast_IsRejectedWhenBuilt(bool generic)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(Parse($"First|{Coalesce}(Quantity, 0)", generic), out _));
        Assert.Contains("argument 1", ex.Message);
    }

    [Theory]
    [InlineData("LastUpdated", "0")]
    [InlineData("Nickname", "0")]
    [InlineData("First", "\"none\"")]
    [InlineData("Day", "0")]
    public void MismatchedTypes_AreRejected(string property, string fallback)
    {
        Assert.Throws<InvalidOperationException>(() => (PropertySelector<TestItem>)$"{property}|{Coalesce}({fallback})");

        PropertySelector nonGeneric = $"{property}|{Coalesce}({fallback})";
        Assert.Throws<InvalidOperationException>(() => Build(nonGeneric, out _));
    }

    [Fact]
    public void MismatchedPropertyFallback_IsRejectedWhenParsed_InTheGenericForm()
    {
        Assert.Throws<InvalidOperationException>(() => (PropertySelector<TestItem>)$"LastUpdated|{Coalesce}(Name)");

        PropertySelector nonGeneric = $"LastUpdated|{Coalesce}(Name)";
        Assert.Throws<InvalidOperationException>(() => Build(nonGeneric, out _));
    }

    [Fact]
    public void Collection_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => (PropertySelector<TestItem>)$"NullableNumbers|{Coalesce}(0)");

        PropertySelector nonGeneric = $"NullableNumbers|{Coalesce}(0)";
        Assert.Throws<InvalidOperationException>(() => Build(nonGeneric, out _));
    }

    [Fact]
    public void WithoutAPipedValue_IsRejected()
    {
        PropertySelector selector = $"{Coalesce}(First, 0)";

        var ex = Assert.Throws<InvalidOperationException>(() => Build(selector, out _));
        Assert.Contains("piped value", ex.Message);
    }

    [Theory]
    [MemberData(nameof(BothForms))]
    public void WithoutArguments_IsRejected(bool generic)
    {
        Assert.Throws<ArgumentException>(() => Parse($"First|{Coalesce}", generic));
    }
}
