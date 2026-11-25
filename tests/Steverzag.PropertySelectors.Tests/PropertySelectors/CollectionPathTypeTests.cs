using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class CollectionPathTypeTests
{
    private class Child
    {
        public string Name { get; set; } = "";
        public int? Score { get; set; }
        public List<string> Tags { get; set; } = [];
        public DateTime? Seen { get; set; }
        public Detail Detail { get; set; } = new();
        public List<Child> Kids { get; set; } = [];
    }

    private class Detail
    {
        public List<string> Codes { get; set; } = [];
    }

    private class Root
    {
        public List<Child> List { get; set; } = [];
        public Child[] Array { get; set; } = [];
        public HashSet<Child> Set { get; set; } = [];
        public ICollection<Child> Collection { get; set; } = [];
        public IEnumerable<Child> Enumerable { get; set; } = [];
        public Child Single { get; set; } = new();
    }

    public static TheoryData<string, Type> Paths() => new()
    {
        { "List.Name", typeof(IEnumerable<string>) },
        { "Array.Name", typeof(IEnumerable<string>) },
        { "Set.Name", typeof(IEnumerable<string>) },
        { "Collection.Name", typeof(IEnumerable<string>) },
        { "Enumerable.Name", typeof(IEnumerable<string>) },
        { "List.Score", typeof(IEnumerable<int?>) },
        { "List.Tags", typeof(IEnumerable<string>) },
        { "List.Seen.Year", typeof(IEnumerable<int?>) },
        { "List.Detail.Codes", typeof(IEnumerable<string>) },
        { "List.Kids.Tags", typeof(IEnumerable<string>) },
        { "List.Kids.Name", typeof(IEnumerable<string>) },
        { "Single.Tags", typeof(List<string>) },
        { "List", typeof(List<Child>) },
        { "Array", typeof(Child[]) },
    };

    [Theory]
    [InlineData("List.Detail.Codes", new[] { "a", "b", "c" })]
    [InlineData("List.Kids.Tags", new[] { "x", "y", "z" })]
    public void NestedCollections_AreFlattened_InBothForms(string text, string[] expected)
    {
        var root = new Root
        {
            List =
            [
                new() { Detail = new() { Codes = ["a", "b"] }, Kids = [new() { Tags = ["x"] }, new() { Tags = ["y"] }] },
                new() { Detail = new() { Codes = ["c"] }, Kids = [new() { Tags = ["z"] }] },
            ],
        };
        PropertySelector<Root> generic = text;
        PropertySelector nonGeneric = text;
        var parameter = Expression.Parameter(typeof(Root), "r");

        foreach (var selector in new PropertySelector[] { generic, nonGeneric })
        {
            var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
            var result = Expression.Lambda<Func<Root, IEnumerable<string>>>(body, parameter).Compile()(root);
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void DeclaredType_MatchesTheBuiltExpression(string text, Type expected)
    {
        PropertySelector<Root> generic = text;
        PropertySelector nonGeneric = text;
        var parameter = Expression.Parameter(typeof(Root), "r");

        Assert.Equal(expected, generic.SelectorType);
        Assert.Equal(expected, ExpressionUtils.CreateMemberAccess(parameter, generic).Type);
        Assert.Equal(expected, ExpressionUtils.CreateMemberAccess(parameter, nonGeneric).Type);
    }
}
