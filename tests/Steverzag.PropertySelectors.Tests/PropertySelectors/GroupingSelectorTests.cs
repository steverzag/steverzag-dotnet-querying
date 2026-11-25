using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class GroupingSelectorTests
{
    public class GroupKey
    {
        public string Name { get; set; } = "Tools";
    }

    public class Product
    {
        public int Quantity { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    public class Order
    {
        public List<Product> Products { get; set; } = [new() { Quantity = 1 }, new() { Quantity = 2 }];
        public string[] Names { get; set; } = ["ab", "cde"];
    }

    private static readonly GroupKey Key = new();

    private static IGrouping<GroupKey, Product> Group() =>
        new[]
        {
            new Product { Quantity = 3, Tags = ["a", "b"] },
            new Product { Quantity = 4, Tags = ["c"] },
        }.GroupBy(_ => Key).Single();

    private static Expression Body<TRoot>(PropertySelector selector, out ParameterExpression parameter)
    {
        parameter = Expression.Parameter(typeof(TRoot), "g");
        return ExpressionUtils.CreateMemberAccess(parameter, selector);
    }

    private static object? Evaluate<TRoot>(PropertySelector selector, TRoot root)
    {
        var body = Body<TRoot>(selector, out var parameter);
        return Expression.Lambda(body, parameter).Compile().DynamicInvoke(root);
    }

    // ── Over a grouping: its own members first, then its elements ───────────

    public static TheoryData<string, object?> GroupValues => new()
    {
        { "Key", Key },
        { "Key.Name", "Tools" },
        { "Key|Name", "Tools" },
        { "Key.Name|$FN_STR_LENGTH", 5 },
        { "Key.Name|$FN_STR_TOUPPER", "TOOLS" },
        { "Quantity|$FN_AGGR_SUM", 7 },
        { "Quantity|$FN_AGGR_MAX", 4 },
        { "Tags|$FN_AGGR_COUNT", 3 },
        { "$FN_AGGR_COUNT", 2 },
    };

    [Theory]
    [MemberData(nameof(GroupValues))]
    public void GenericForm_ResolvesAGroupingsKeyAndElements(string text, object? expected)
    {
        PropertySelector<IGrouping<GroupKey, Product>> selector = text;

        Assert.Equal(Body<IGrouping<GroupKey, Product>>(selector, out _).Type, selector.Type);
        Assert.Equal(expected, Evaluate(selector, Group()));
    }

    [Theory]
    [MemberData(nameof(GroupValues))]
    public void NonGenericForm_ResolvesAGroupingsKeyAndElements(string text, object? expected)
    {
        PropertySelector selector = text;

        Assert.Equal(expected, Evaluate(selector, Group()));
    }

    [Fact]
    public void GenericForm_DeclaresTheKeyType()
    {
        PropertySelector<IGrouping<GroupKey, Product>> key = "Key";
        PropertySelector<IGrouping<GroupKey, Product>> name = "Key.Name";

        Assert.Equal(typeof(GroupKey), key.Type);
        Assert.Equal(typeof(string), name.Type);
    }

    [Fact]
    public void GenericForm_ProjectsAPropertyOfTheElements()
    {
        PropertySelector<IGrouping<GroupKey, Product>> selector = "Quantity";

        Assert.Equal(typeof(IEnumerable<int>), selector.Type);
        Assert.Equal(new[] { 3, 4 }, (IEnumerable<int>)Evaluate(selector, Group())!);
    }

    [Fact]
    public void GenericForm_RejectsAMemberOnNeitherTheGroupingNorItsElements()
    {
        var e = Assert.Throws<SelectorParseException>(() => { PropertySelector<IGrouping<GroupKey, Product>> selector = "Key|Missing"; });

        Assert.Equal("Property 'Missing' was not found on type 'GroupKey'.", e.Detail);
        Assert.Equal(4, e.Position);
    }

    [Fact]
    public void GenericForm_RejectsAnElementMemberThatDoesNotExist()
    {
        var e = Assert.Throws<SelectorParseException>(() => { PropertySelector<IGrouping<GroupKey, Product>> selector = "Missing|$FN_AGGR_SUM"; });

        Assert.Equal("Property 'Missing' was not found on type 'Product'.", e.Detail);
    }

    // ── Any collection at the start of a part: the same rule ───────────────

    public static TheoryData<string, object?> CollectionValues => new()
    {
        { "Names|Length", 2 },
        { "Products|Count", 2 },
        { "Products|Quantity|$FN_AGGR_SUM", 3 },
    };

    [Theory]
    [MemberData(nameof(CollectionValues))]
    public void APipedCollection_ReadsItsOwnMemberFirst(string text, object? expected)
    {
        PropertySelector<Order> generic = text;
        PropertySelector nonGeneric = text;

        Assert.Equal(Body<Order>(generic, out _).Type, generic.Type);
        Assert.Equal(expected, Evaluate(generic, new Order()));
        Assert.Equal(expected, Evaluate(nonGeneric, new Order()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InsideADottedPath_ACollectionStillProjects(bool generic)
    {
        PropertySelector selector = generic ? (PropertySelector<Order>)"Names.Length" : (PropertySelector)"Names.Length";

        Assert.Equal(typeof(IEnumerable<int>), Body<Order>(selector, out _).Type);
        Assert.Equal(new[] { 2, 3 }, (IEnumerable<int>)Evaluate(selector, new Order())!);
    }
}
