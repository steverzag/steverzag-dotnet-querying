using Steverzag.PropertySelectors.MetaData;

namespace Steverzag.PropertySelectors.Tests.MetaData;

public class TypeDisplayTests
{
    public class Phone;

    [Theory]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(decimal?), "decimal?")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(object), "object")]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(DateTime), "DateTime")]
    [InlineData(typeof(DateOnly?), "DateOnly?")]
    [InlineData(typeof(DayOfWeek), "DayOfWeek")]
    [InlineData(typeof(Phone), "Phone")]
    [InlineData(typeof(List<int>), "List<int>")]
    [InlineData(typeof(IEnumerable<decimal?>), "IEnumerable<decimal?>")]
    [InlineData(typeof(IOrderedEnumerable<Phone>), "IOrderedEnumerable<Phone>")]
    [InlineData(typeof(Dictionary<string, List<int?>>), "Dictionary<string, List<int?>>")]
    [InlineData(typeof(int[]), "int[]")]
    [InlineData(typeof(string[,]), "string[,]")]
    [InlineData(typeof(List<int>[]), "List<int>[]")]
    public void Name_ReadsLikeCSharp(Type type, string expected)
    {
        Assert.Equal(expected, TypeDisplay.Name(type));
    }

    [Fact]
    public void Name_OfNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TypeDisplay.Name(null!));
    }

    [Fact]
    public void AFunctionMatch_NamesItsResultType()
    {
        var matches = FunctionCatalog.After(typeof(List<int>));

        Assert.Equal("double?", matches.First(m => m.Function.Token == "$FN_AGGR_AVG").ResultTypeName);
        Assert.Equal("IOrderedEnumerable<int>", matches.First(m => m.Function.Token == "$FN_ORDER_ASC").ResultTypeName);
        Assert.Null(FunctionCatalog.After(typeof(decimal)).First(m => m.Function.Token == "$FN_MATH_MAX").ResultTypeName);
    }
}
