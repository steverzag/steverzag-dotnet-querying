using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class BuildTimeTypeErrorTests
{
    public class Line
    {
        public int Id { get; set; }
    }

    public class Item
    {
        public int Age { get; set; } = 30;
        public string Name { get; set; } = "Ann";
        public DateTime Created { get; set; }
        public List<Line> Lines { get; set; } = [new()];
        public List<string> Tags { get; set; } = ["a"];
    }

    private static void Build(PropertySelector selector)
        => SelectorExpressions.CreateMemberAccess(Expression.Parameter(typeof(Item), "x"), selector);

    public static TheoryData<string> Mismatches =>
    [
        "true|$FN_STR_LENGTH",
        "Age|$FN_STR_TRIM",
        "Created|$FN_STR_TOUPPER",
        "$FN_AGGR_COUNT",
        "Age|$FN_AGGR_SUM",
        "Tags|$FN_AGGR_SUM",
        "Tags|$FN_AGGR_AVG",
        "Age|$FN_ELM_FIRST",
        "Name|$FN_MATH_SQRT",
        "Lines|$FN_MATH_ROUND",
        "$FN_MATH_MAX(Name, 1)",
        "Age|$FN_MATH_POW(Name)",
        "Name|$FN_DT_ADDDAYS(1)",
        "Created|$FN_DT_ADDDAYS(Name)",
        "Name|$FN_STR_CONCAT(Age)",
        "Age|$FN_COALESCE(Name)",
        "Lines|$FN_ORDER_ASC(Id)|$FN_STR_LENGTH",
    ];

    [Theory]
    [MemberData(nameof(Mismatches))]
    public void ATypeMismatch_IsASelectorParseException_WithTheSameDetailInBothForms(string text)
    {
        var generic = Assert.Throws<SelectorParseException>(() => (PropertySelector<Item>)text);

        PropertySelector nonGeneric = text;
        var whenBuilt = Assert.Throws<SelectorParseException>(() => Build(nonGeneric));

        Assert.Equal(generic.Detail, whenBuilt.Detail);
    }

    [Theory]
    [InlineData("true|$FN_STR_LENGTH", "$FN_STR_LENGTH", 0, "$FN_STR_LENGTH")]
    [InlineData("Name|$FN_MATH_SQRT", "Name|$FN_MATH_SQRT", 5, "$FN_MATH_SQRT")]
    [InlineData("$FN_MATH_MAX(Name, 1)", "$FN_MATH_MAX(Name, 1)", 0, "$FN_MATH_MAX(Name, 1)")]
    [InlineData("Created|$FN_DT_ADDDAYS(Name)", "Created|$FN_DT_ADDDAYS(Name)", 8, "$FN_DT_ADDDAYS(Name)")]
    public void WhenBuilt_TheErrorPointsAtTheFunction_InItsOwnText(string text, string selector, int position, string token)
    {
        PropertySelector nonGeneric = text;
        var exception = Assert.Throws<SelectorParseException>(() => Build(nonGeneric));

        Assert.Equal(selector, exception.Selector);
        Assert.Equal(position, exception.Position);
        Assert.Equal(token, exception.Token);
    }

    [Theory]
    [InlineData("Lines|$FN_AGGR_COUNT")]
    [InlineData("Name|$FN_STR_LENGTH")]
    [InlineData("Age|$FN_MATH_SQRT")]
    [InlineData("Created|$FN_DT_ADDDAYS(Age)")]
    [InlineData("$FN_DT_DATEONLY(\"2024-01-31\")")]
    [InlineData("$FN_DT_NOW|Year")]
    public void AWellTypedNonGenericSelector_StillBuilds(string text)
    {
        PropertySelector nonGeneric = text;

        Build(nonGeneric);
    }
}
