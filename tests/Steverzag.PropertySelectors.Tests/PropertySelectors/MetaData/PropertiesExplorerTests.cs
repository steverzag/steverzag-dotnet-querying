using Steverzag.PropertySelectors.MetaData;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.MetaData;

public class PropertiesExplorerTests
{
    public class Line
    {
        public decimal Amount { get; set; }
        public List<string> Notes { get; set; } = [];
    }

    public class Order
    {
        public List<Line> Lines { get; set; } = [];
        public DateOnly? Shipped { get; set; }
    }

    public class Phone
    {
        public string Number { get; set; } = "";
        public bool True { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime? Deleted { get; set; }
        public List<Phone> Phones { get; set; } = [];
        public int[] Scores { get; set; } = [];
        public List<Order> Orders { get; set; } = [];
        public List<Person> Friends { get; set; } = [];
        public Person? Boss { get; set; }
        public Phone Main { get; set; } = new();
        public int this[int index] => index;
        public int Field;
    }

    private static readonly SelectorsMetaDataTree Tree = new PropertiesExplorer<Person>().ExplorePropertiesTree(4);

    private static IEnumerable<SelectorsMetaDataTree> Nodes(SelectorsMetaDataTree node) =>
        node.Selectors.SelectMany(child => Nodes(child).Prepend(child));

    private static SelectorsMetaDataTree Node(string path) => Nodes(Tree).Single(n => n.Path == path);

    private static List<string> Children(string path) => Node(path).Selectors.Select(n => n.Name).ToList();

    [Fact]
    public void TheRoot_IsTheExploredType_WithAnEmptyPath()
    {
        Assert.Equal("Root", Tree.Name);
        Assert.Equal("", Tree.Path);
        Assert.Equal(typeof(Person), Tree.SelectorType);
        Assert.False(Tree.IsCollection);
    }

    [Fact]
    public void TheRoot_ListsPublicProperties_WithoutIndexersOrFields()
    {
        Assert.Equal(["Id", "Name", "Deleted", "Phones", "Scores", "Orders", "Friends", "Boss", "Main"], Tree.Selectors.Select(n => n.Name));
    }

    [Fact]
    public void ACollection_ListsItsElementsProperties_NotItsOwn()
    {
        Assert.Equal(["Number", "True"], Children("Phones"));
        Assert.Empty(Children("Scores"));
    }

    [Fact]
    public void ANullableValue_ListsItsUnderlyingProperties()
    {
        var children = Children("Deleted");

        Assert.Contains("Year", children);
        Assert.Contains("Date", children);
        Assert.DoesNotContain("HasValue", children);
        Assert.DoesNotContain("Value", children);
    }

    [Theory]
    [InlineData("Id", typeof(int))]
    [InlineData("Deleted.Year", typeof(int?))]
    [InlineData("Phones.Number", typeof(IEnumerable<string>))]
    [InlineData("Orders.Lines", typeof(IEnumerable<Line>))]
    [InlineData("Orders.Lines.Notes", typeof(IEnumerable<string>))]
    [InlineData("Orders.Shipped.Year", typeof(IEnumerable<int?>))]
    public void ANode_DeclaresTheTypeOfTheSelectorItsPathMakes(string path, Type expected)
    {
        Assert.Equal(expected, Node(path).SelectorType);
    }

    [Theory]
    [InlineData("Phones", typeof(Phone))]
    [InlineData("Scores", typeof(int))]
    [InlineData("Phones.Number", typeof(string))]
    [InlineData("Orders.Lines.Amount", typeof(decimal))]
    [InlineData("Friends", typeof(Person))]
    public void ACollectionNode_IsFlagged_WithItsElementType(string path, Type element)
    {
        var node = Node(path);

        Assert.True(node.IsCollection);
        Assert.Equal(element, node.ElementType);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Name")]
    [InlineData("Deleted")]
    [InlineData("Main")]
    public void ANodeThatIsNotACollection_HasNoElementType(string path)
    {
        var node = Node(path);

        Assert.False(node.IsCollection);
        Assert.Null(node.ElementType);
        Assert.Null(node.ElementTypeName);
    }

    [Fact]
    public void ANameThatReadsAsALiteral_IsEscapedInThePath()
    {
        Assert.Equal(["Main.@True", "Phones.@True"], Nodes(Tree).Where(n => n.Name == "True").Select(n => n.Path).Order());
    }

    [Fact]
    public void ACycle_EndsAsALeaf()
    {
        Assert.Empty(Children("Friends"));
        Assert.Empty(Children("Boss"));
        Assert.Empty(Children("Deleted.Date"));
        Assert.Equal(["Number", "True"], Children("Main"));
    }

    [Fact]
    public void MaxDepth_LimitsTheLevels()
    {
        var tree = new PropertiesExplorer<Person>().ExplorePropertiesTree(1);

        Assert.NotEmpty(tree.Selectors);
        Assert.All(tree.Selectors, n => Assert.Empty(n.Selectors));
    }

    [Theory]
    [InlineData("Id", "int", null)]
    [InlineData("Deleted", "DateTime?", null)]
    [InlineData("Phones", "List<Phone>", "Phone")]
    [InlineData("Orders.Shipped", "IEnumerable<DateOnly?>", "DateOnly?")]
    public void ANode_HasDisplayNames(string path, string typeName, string? elementTypeName)
    {
        var node = Node(path);

        Assert.Equal(typeName, node.TypeName);
        Assert.Equal(elementTypeName, node.ElementTypeName);
    }

    [Fact]
    public void EveryNode_ParsesAndBuilds_ToItsType_InBothForms()
    {
        var parameter = Expression.Parameter(typeof(Person), "p");

        foreach (var node in Nodes(Tree))
        {
            var generic = (PropertySelector<Person>)node.Path;
            Assert.True(node.SelectorType == generic.Type, $"{node.Path}: the tree says {node.SelectorType}, parsing declares {generic.Type}.");

            foreach (PropertySelector selector in new[] { generic, (PropertySelector)node.Path })
            {
                var built = SelectorExpressions.CreateMemberAccess(parameter, selector).Type;
                Assert.True(node.SelectorType == built, $"{node.Path}: the tree says {node.SelectorType}, building gives {built}.");
            }
        }
    }

    [Fact]
    public void ACollectionNode_IsOfferedTheAggregates()
    {
        foreach (var node in Nodes(Tree).Where(n => n.IsCollection))
            Assert.Contains(FunctionCatalog.After(node.SelectorType), m => m.Function.Token == "$FN_AGGR_COUNT");
    }

    [Fact]
    public void ExploringTwice_ReturnsTwoIndependentTrees()
    {
        var explorer = new PropertiesExplorer<Person>();

        var first = explorer.ExplorePropertiesTree(2);
        var second = explorer.ExplorePropertiesTree(2, MemberFilter.All.Hide<Person>(p => p.Name));

        Assert.NotSame(first, second);
        Assert.Equal(first.Selectors.Count, first.Selectors.Select(n => n.Path).Distinct().Count());
        Assert.Contains(first.Selectors, n => n.Path == "Name");
        Assert.DoesNotContain(second.Selectors, n => n.Path == "Name");
        Assert.Equal(first.Selectors.Count - 1, second.Selectors.Count);
    }
}
