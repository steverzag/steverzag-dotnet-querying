using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class CaseSensitivePropertyTests
{
    public struct Point
    {
        public int Y { get; set; }
    }

    public class Inner
    {
        public int Value { get; set; } = 7;
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public Inner Inner { get; set; } = new();
        public List<Inner> Items { get; set; } = [new()];
        public Point? Location { get; set; } = new Point { Y = 2 };
        internal int Secret { get; set; } = 1;
    }

    public class Twin
    {
        public int Code { get; set; } = 1;
        public string code { get; set; } = "lower";
    }

    public class Base
    {
        public int Id { get; set; } = 1;
    }

    public class Derived : Base
    {
        public new string Id { get; set; } = "derived";
    }

    private static object? Evaluate<TRoot>(string text, bool generic, TRoot root, out Type declared, out Type built)
    {
        PropertySelector selector = generic ? (PropertySelector<TRoot>)text : (PropertySelector)text;
        var parameter = Expression.Parameter(typeof(TRoot), "x");
        var body = SelectorExpressions.CreateMemberAccess(parameter, selector);

        declared = selector.Type!;
        built = body.Type;
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(root);
    }

    private static SelectorParseException Rejects<TRoot>(string text, bool generic)
        => Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector selector = generic ? (PropertySelector<TRoot>)text : (PropertySelector)text;
            SelectorExpressions.CreateMemberAccess(Expression.Parameter(typeof(TRoot), "x"), selector);
        });

    public static TheoryData<string, string, string, string, int, bool> WronglyCased
    {
        get
        {
            var data = new TheoryData<string, string, string, string, int, bool>();
            foreach (var generic in new[] { true, false })
            {
                data.Add("age", "age", "Item", "Age", 0, generic);
                data.Add("inner.value", "inner", "Item", "Inner", 0, generic);
                data.Add("Inner.value", "value", "Inner", "Value", 6, generic);
                data.Add("Items.value", "value", "Inner", "Value", 6, generic);
                data.Add("Location.y", "y", "Point", "Y", 9, generic);
                data.Add("Name|length", "length", "String", "Length", generic ? 5 : 0, generic);
                data.Add("Items|$FN_ELM_FIRST|value", "value", "Inner", "Value", generic ? 20 : 0, generic);
                data.Add("Age + age", "age", "Item", "Age", generic ? 6 : 0, generic);
                data.Add("$FN_MATH_MAX(Age, age)", "age", "Item", "Age", generic ? 18 : 0, generic);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(WronglyCased))]
    public void AWronglyCasedName_IsNotFound_AndNamesTheRealOne(string text, string token, string type, string real, int position, bool generic)
    {
        var exception = Rejects<Item>(text, generic);

        Assert.Equal($"Property '{token}' was not found on type '{type}'. Property names are case-sensitive: '{real}'.", exception.Detail);
        Assert.Equal(token, exception.Token);
        Assert.Equal(position, exception.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUnrelatedName_GetsNoHint(bool generic)
    {
        var exception = Rejects<Item>("Missing", generic);

        Assert.Equal("Property 'Missing' was not found on type 'Item'.", exception.Detail);
    }

    [Theory]
    [InlineData("Age", 30, true)]
    [InlineData("Age", 30, false)]
    [InlineData("Inner.Value", 7, true)]
    [InlineData("Inner.Value", 7, false)]
    [InlineData("Location.Y", 2, true)]
    [InlineData("Location.Y", 2, false)]
    [InlineData("Name|Length", 3, true)]
    [InlineData("Name|Length", 3, false)]
    public void TheExactName_Resolves(string text, object expected, bool generic)
    {
        Assert.Equal(expected, Evaluate(text, generic, new Item(), out _, out _));
    }

    [Theory]
    [InlineData("Code", 1, typeof(int), true)]
    [InlineData("Code", 1, typeof(int), false)]
    [InlineData("code", "lower", typeof(string), true)]
    [InlineData("code", "lower", typeof(string), false)]
    public void PropertiesThatDifferOnlyByCase_AreTwoProperties(string text, object expected, Type type, bool generic)
    {
        Assert.Equal(expected, Evaluate(text, generic, new Twin(), out var declared, out var built));
        if (generic)
            Assert.Equal(type, declared);
        Assert.Equal(type, built);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHidingProperty_ResolvesToTheMostDerived(bool generic)
    {
        Assert.Equal("derived", Evaluate("Id", generic, new Derived(), out var declared, out var built));
        if (generic)
            Assert.Equal(typeof(string), declared);
        Assert.Equal(typeof(string), built);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANonPublicProperty_IsNotFound(bool generic)
    {
        var exception = Rejects<Item>("Secret", generic);

        Assert.Equal("Property 'Secret' was not found on type 'Item'.", exception.Detail);
    }
}
