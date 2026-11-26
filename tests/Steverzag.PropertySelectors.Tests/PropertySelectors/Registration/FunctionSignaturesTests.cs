using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.Registration;
using System.Reflection;

namespace Steverzag.PropertySelectors.Tests.Registration;

public record TestUnsignedSelector<T> : FunctionPropertySelector<T>
{
    public const string Token = "$FN_TESTSIG_UNSIGNED";
    public TestUnsignedSelector() : base(Token) { }
    public TestUnsignedSelector(List<PropertySelector> parameters) : base(Token, parameters) { }
}

public record TestUnsignedSelector : FunctionPropertySelector
{
    public TestUnsignedSelector() : base(TestUnsignedSelector<object>.Token) { }
    public TestUnsignedSelector(List<PropertySelector> parameters) : base(TestUnsignedSelector<object>.Token, parameters) { }
}

public class FunctionSignaturesTests
{
    private const string Message = "declares no signatures. A function selector must implement IFunctionSignatures.";

    public class Item
    {
        public decimal Price { get; set; }
    }

    public static TheoryData<Type, Type> BuiltInFunctions()
    {
        var data = new TheoryData<Type, Type>();
        var types = typeof(SelectorRegistry).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<SelectorTokenAttribute>() is not null)
            .ToList();

        foreach (var generic in types.Where(t => t.IsGenericTypeDefinition))
        {
            var token = generic.GetCustomAttribute<SelectorTokenAttribute>()!.Token;
            var nonGeneric = types.Single(t => !t.IsGenericTypeDefinition && t.GetCustomAttribute<SelectorTokenAttribute>()!.Token == token);
            data.Add(generic, nonGeneric);
        }

        return data;
    }

    [Fact]
    public void EveryBuiltInToken_HasBothForms()
    {
        var tokens = typeof(SelectorRegistry).Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<SelectorTokenAttribute>()?.Token)
            .OfType<string>()
            .ToList();

        Assert.Equal(tokens.Distinct().Count() * 2, tokens.Count);
        Assert.Equal(tokens.Count / 2, BuiltInFunctions().Count());
    }

    [Theory]
    [MemberData(nameof(BuiltInFunctions))]
    public void EveryBuiltInFunction_DeclaresOneTable_ForBothForms(Type generic, Type nonGeneric)
    {
        var genericTable = FunctionSignatures.Of(generic.MakeGenericType(typeof(object)));
        var nonGenericTable = FunctionSignatures.Of(nonGeneric);

        Assert.NotEmpty(nonGenericTable);
        Assert.Same(nonGenericTable, genericTable);
    }

    [Fact]
    public void TheTable_IsReadable_WithoutAnInstance()
    {
        Assert.Equal(2, MathRoundSelector.Signatures.Count);
        Assert.Same(MathRoundSelector.Signatures, MathRoundSelector<Item>.Signatures);
    }

    [Fact]
    public void AResolvedSelector_UsesASignatureFromItsTable()
    {
        var generic = (FunctionPropertySelector<Item>)(PropertySelector<Item>)"$FN_MATH_ROUND(Price, 2)";
        var nonGeneric = (FunctionPropertySelector)(PropertySelector)"$FN_MATH_ROUND(Price, 2)";

        Assert.Same(MathRoundSelector.Signatures[1], generic.Signature);
        Assert.Same(MathRoundSelector.Signatures[1], nonGeneric.Signature);
    }

    [Fact]
    public void Register_RejectsAGenericSelectorWithoutSignatures()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            SelectorRegistry.Register(TestUnsignedSelector<object>.Token, typeof(TestUnsignedSelector<>), null));

        Assert.StartsWith($"The provided type {typeof(TestUnsignedSelector<>).FullName} {Message}", error.Message);
        Assert.Equal("openGenericSelectorType", error.ParamName);
        Assert.False(SelectorRegistry.IsRegistered(TestUnsignedSelector<object>.Token));
    }

    [Fact]
    public void Register_RejectsANonGenericSelectorWithoutSignatures()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            SelectorRegistry.Register(TestUnsignedSelector<object>.Token, null, typeof(TestUnsignedSelector)));

        Assert.StartsWith($"The provided type {typeof(TestUnsignedSelector).FullName} {Message}", error.Message);
        Assert.Equal("nonGenericSelectorType", error.ParamName);
        Assert.False(SelectorRegistry.IsRegistered(TestUnsignedSelector<object>.Token));
    }

    [Fact]
    public void ConstructingASelectorWithoutSignatures_Throws()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new TestUnsignedSelector());

        Assert.Equal($"{typeof(TestUnsignedSelector).FullName} {Message}", error.Message);
    }
}
