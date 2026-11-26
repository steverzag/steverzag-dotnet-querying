using Steverzag.PropertySelectors.MetaData;

namespace Steverzag.PropertySelectors.Tests.Registration;

public class ArgumentConstraintAcceptsTests
{
    public static TheoryData<string, Type, bool> Cases() => new()
    {
        { nameof(ArgumentConstraint.Any), typeof(object), true },
        { nameof(ArgumentConstraint.Any), typeof(DateTime?), true },
        { nameof(ArgumentConstraint.Numeric), typeof(int), true },
        { nameof(ArgumentConstraint.Numeric), typeof(decimal?), true },
        { nameof(ArgumentConstraint.Numeric), typeof(string), false },
        { nameof(ArgumentConstraint.Numeric), typeof(DayOfWeek), false },
        { nameof(ArgumentConstraint.Integer), typeof(long), true },
        { nameof(ArgumentConstraint.Integer), typeof(int?), true },
        { nameof(ArgumentConstraint.Integer), typeof(double), false },
        { nameof(ArgumentConstraint.String), typeof(string), true },
        { nameof(ArgumentConstraint.String), typeof(char), false },
        { nameof(ArgumentConstraint.Bool), typeof(bool?), true },
        { nameof(ArgumentConstraint.Bool), typeof(int), false },
        { nameof(ArgumentConstraint.DateLike), typeof(DateOnly), true },
        { nameof(ArgumentConstraint.DateLike), typeof(TimeOnly?), true },
        { nameof(ArgumentConstraint.DateLike), typeof(TimeSpan), false },
        { nameof(ArgumentConstraint.Collection), typeof(List<int>), true },
        { nameof(ArgumentConstraint.Collection), typeof(int[]), true },
        { nameof(ArgumentConstraint.Collection), typeof(string), false },
        { nameof(ArgumentConstraint.Collection), typeof(int), false },
        { nameof(ArgumentConstraint.NumericCollection), typeof(List<decimal?>), true },
        { nameof(ArgumentConstraint.NumericCollection), typeof(List<string>), false },
        { nameof(ArgumentConstraint.StringLiteral), typeof(string), false },
        { nameof(ArgumentConstraint.IntegerLiteral), typeof(int), false },
    };

    private static ArgumentConstraint Named(string name) =>
        (ArgumentConstraint)typeof(ArgumentConstraint).GetProperty(name)!.GetValue(null)!;

    [Theory]
    [MemberData(nameof(Cases))]
    public void Accepts_TellsWhetherAValueOfTheTypeFits(string constraint, Type type, bool expected)
    {
        Assert.Equal(expected, Named(constraint).Accepts(type));
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(long?), true)]
    [InlineData(typeof(double), false)]
    [InlineData(typeof(string), false)]
    public void Of_AcceptsTheTypeAndImplicitNumericConversions(Type type, bool expected)
    {
        Assert.Equal(expected, ArgumentConstraint.Of(typeof(long)).Accepts(type));
    }

    [Fact]
    public void Family_AcceptsTheUnderlyingTypeOfANullable()
    {
        var guid = ArgumentConstraint.Family("a Guid", type => type == typeof(Guid));

        Assert.True(guid.Accepts(typeof(Guid?)));
        Assert.False(guid.Accepts(typeof(string)));
    }

    [Fact]
    public void Accepts_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ArgumentConstraint.Any.Accepts(null!));
    }

    [Fact]
    public void ALiteralOnlyArgument_AcceptsNoType_AndSaysSo()
    {
        var atIndex = Assert.Single(FunctionCatalog.After(typeof(List<int>)), m => m.Function.Token == "$FN_ELM_ATINDEX");
        var index = Assert.Single(atIndex.Parameters);

        Assert.True(index.IsLiteral);
        Assert.False(index.Accepts(typeof(int)));
    }

    [Fact]
    public void Accepts_PicksThePropertiesThatFitAnArgument()
    {
        var round = FunctionCatalog.After(typeof(decimal)).Single(m => m.Function.Token == "$FN_MATH_ROUND" && m.Parameters.Count == 1);
        var properties = typeof(Item).GetProperties();

        var fitting = properties.Where(p => round.Parameters[0].Accepts(p.PropertyType)).Select(p => p.Name);

        Assert.Equal([nameof(Item.Digits), nameof(Item.MaybeDigits)], fitting);
    }

    public class Item
    {
        public int Digits { get; set; }
        public int? MaybeDigits { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; } = "";
    }
}
