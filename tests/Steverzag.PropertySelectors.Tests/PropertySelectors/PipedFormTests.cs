using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests;

public class PipedFormTests
{
    public class Product
    {
        public decimal Price { get; set; }
        public double Ratio { get; set; }
        public int Quantity { get; set; }
        public int Digits { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public DateTime Created { get; set; }
        public DateOnly Day { get; set; }
        public DateTimeOffset Stamp { get; set; }
        public List<int> Numbers { get; set; } = [];
        public ProductDetails Details { get; set; } = new();
    }

    public class ProductDetails
    {
        public decimal Price { get; set; }
    }

    private static readonly Product Sample = new()
    {
        Price = 12.3456m,
        Ratio = 0.5,
        Quantity = 7,
        Digits = 1,
        Name = " Lamp ",
        Nickname = null,
        Created = new DateTime(2024, 3, 15, 10, 30, 0),
        Day = new DateOnly(2024, 3, 15),
        Stamp = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.FromHours(2)),
        Numbers = [4, 5, 7],
        Details = new() { Price = 9.87m },
    };

    public static TheoryData<string, string> UnaryMath => new()
    {
        { "Ratio|$FN_MATH_ACOS", "$FN_MATH_ACOS(Ratio)" },
        { "Ratio|$FN_MATH_ASIN", "$FN_MATH_ASIN(Ratio)" },
        { "Ratio|$FN_MATH_ATAN", "$FN_MATH_ATAN(Ratio)" },
        { "Ratio|$FN_MATH_CEILING", "$FN_MATH_CEILING(Ratio)" },
        { "Ratio|$FN_MATH_COS", "$FN_MATH_COS(Ratio)" },
        { "Ratio|$FN_MATH_EXP", "$FN_MATH_EXP(Ratio)" },
        { "Ratio|$FN_MATH_FLOOR", "$FN_MATH_FLOOR(Ratio)" },
        { "Ratio|$FN_MATH_LOG10", "$FN_MATH_LOG10(Ratio)" },
        { "Ratio|$FN_MATH_LOG", "$FN_MATH_LOG(Ratio)" },
        { "Ratio|$FN_MATH_ROUND", "$FN_MATH_ROUND(Ratio)" },
        { "Ratio|$FN_MATH_SIGN", "$FN_MATH_SIGN(Ratio)" },
        { "Ratio|$FN_MATH_SIN", "$FN_MATH_SIN(Ratio)" },
        { "Ratio|$FN_MATH_SQRT", "$FN_MATH_SQRT(Ratio)" },
        { "Ratio|$FN_MATH_TAN", "$FN_MATH_TAN(Ratio)" },
        { "Ratio|$FN_MATH_TRUNCATE", "$FN_MATH_TRUNCATE(Ratio)" },
    };

    public static TheoryData<string, string> WithArguments => new()
    {
        { "Price|$FN_MATH_ROUND(2)", "$FN_MATH_ROUND(Price, 2)" },
        { "Price|$FN_MATH_ROUND(Digits)", "$FN_MATH_ROUND(Price, Digits)" },
        { "Details.Price|$FN_MATH_ROUND(Digits)", "$FN_MATH_ROUND(Details.Price, Digits)" },
        { "Ratio|$FN_MATH_POW(2)", "$FN_MATH_POW(Ratio, 2)" },
        { "Ratio|$FN_MATH_ATAN2(1)", "$FN_MATH_ATAN2(Ratio, 1)" },
        { "Quantity|$FN_MATH_MAX(10)", "$FN_MATH_MAX(Quantity, 10)" },
        { "Quantity|$FN_MATH_MIN(Digits)", "$FN_MATH_MIN(Quantity, Digits)" },
        { "Price|$FN_MATH_MAX(Ratio)", "$FN_MATH_MAX(Price, Ratio)" },
        { "Price|$FN_MATH_CEILING", "$FN_MATH_CEILING(Price)" },
        { "Price|$FN_MATH_FLOOR", "$FN_MATH_FLOOR(Price)" },
        { "Price|$FN_MATH_TRUNCATE", "$FN_MATH_TRUNCATE(Price)" },
        { "Price|$FN_MATH_ROUND", "$FN_MATH_ROUND(Price)" },
        { "Quantity|$FN_MATH_ROUND", "$FN_MATH_ROUND(Quantity)" },
        { "Name|$FN_STR_CONCAT(\"!\")", "$FN_STR_CONCAT(Name, \"!\")" },
        { "Name|$FN_STR_CONCAT(\"-\", Nickname)", "$FN_STR_CONCAT(Name, \"-\", Nickname)" },
        { "Name|$FN_STR_CONCAT(\"a\", \"b\", \"c\")", "$FN_STR_CONCAT(Name, \"a\", \"b\", \"c\")" },
        { "Name|$FN_STR_COMPARE(\"b\")", "$FN_STR_COMPARE(Name, \"b\")" },
        { "Nickname|$FN_STR_ISNULLOREMPTY", "$FN_STR_ISNULLOREMPTY(Nickname)" },
        { "Name|$FN_STR_ISNULLORWHITESPACE", "$FN_STR_ISNULLORWHITESPACE(Name)" },
        { "Created|$FN_DT_ADDYEARS(1)", "$FN_DT_ADDYEARS(Created, 1)" },
        { "Created|$FN_DT_ADDMONTHS(1)", "$FN_DT_ADDMONTHS(Created, 1)" },
        { "Created|$FN_DT_ADDDAYS(3)", "$FN_DT_ADDDAYS(Created, 3)" },
        { "Created|$FN_DT_ADDHOURS(Quantity)", "$FN_DT_ADDHOURS(Created, Quantity)" },
        { "Created|$FN_DT_ADDMINUTES(1)", "$FN_DT_ADDMINUTES(Created, 1)" },
        { "Created|$FN_DT_ADDSECONDS(1)", "$FN_DT_ADDSECONDS(Created, 1)" },
        { "Created|$FN_DT_ADDMILLISECONDS(1)", "$FN_DT_ADDMILLISECONDS(Created, 1)" },
        { "Day|$FN_DT_ADDDAYS(Quantity)", "$FN_DT_ADDDAYS(Day, Quantity)" },
        { "Stamp|$FN_DT_ADDHOURS(1)", "$FN_DT_ADDHOURS(Stamp, 1)" },
    };

    public static TheoryData<string, string> InContext => new()
    {
        { "Name|$FN_STR_TRIM|$FN_STR_CONCAT(\"!\")", "$FN_STR_CONCAT(Name|$FN_STR_TRIM, \"!\")" },
        { "Numbers|$FN_AGGR_SUM|$FN_MATH_SQRT", "$FN_MATH_SQRT(Numbers|$FN_AGGR_SUM)" },
        { "Nickname|$FN_COALESCE(Name)|$FN_STR_CONCAT(\"!\")", "$FN_STR_CONCAT(Nickname|$FN_COALESCE(Name), \"!\")" },
        { "Price|$FN_MATH_ROUND(2) * 3", "$FN_MATH_ROUND(Price, 2) * 3" },
        { "Quantity|$FN_MATH_MAX(10) - Digits", "$FN_MATH_MAX(Quantity, 10) - Digits" },
        { "Created|$FN_DT_ADDDAYS(1)|$FN_DT_ADDHOURS(2)", "$FN_DT_ADDHOURS($FN_DT_ADDDAYS(Created, 1), 2)" },
        { "Ratio|$FN_MATH_POW(2)|$FN_MATH_SQRT", "$FN_MATH_SQRT($FN_MATH_POW(Ratio, 2))" },
        { "Nickname|$FN_COALESCE(Name|$FN_STR_CONCAT(\"?\"))", "Nickname|$FN_COALESCE($FN_STR_CONCAT(Name, \"?\"))" },
    };

    [Theory]
    [MemberData(nameof(UnaryMath))]
    [MemberData(nameof(WithArguments))]
    [MemberData(nameof(InContext))]
    public void PipedForm_BuildsTheSameExpressionAsTheStaticForm(string piped, string @static)
    {
        foreach (var (pipedSelector, staticSelector) in BothForms(piped).Zip(BothForms(@static)))
        {
            var pipedBody = Body(pipedSelector);
            var staticBody = Body(staticSelector);

            Assert.Equal(staticBody.ToString(), pipedBody.ToString());
            Assert.Equal(Evaluate(staticBody), Evaluate(pipedBody));
        }
    }

    [Theory]
    [MemberData(nameof(UnaryMath))]
    [MemberData(nameof(WithArguments))]
    [MemberData(nameof(InContext))]
    public void BothForms_DeclareTheTypeTheyBuild(string piped, string @static)
    {
        foreach (var text in new[] { piped, @static })
        {
            var selector = (PropertySelector<Product>)text;

            Assert.NotNull(selector.Type);
            Assert.Equal(Body(selector).Type, selector.Type);
        }
    }

    [Theory]
    [InlineData("Price|$FN_MATH_ROUND(2)", 12.35)]
    [InlineData("Details.Price|$FN_MATH_ROUND(Digits)", 9.9)]
    [InlineData("Price|$FN_MATH_ROUND(2) * 3", 37.05)]
    [InlineData("Quantity|$FN_MATH_MIN(Digits)", 1)]
    public void PipedForm_GivesTheExpectedValue(string text, double expected)
    {
        foreach (var selector in BothForms(text))
            Assert.Equal(expected, Convert.ToDouble(Evaluate(Body(selector))), 10);
    }

    public static TheoryData<string, object> ExactlyTyped => new()
    {
        { "Price|$FN_MATH_ROUND(2)", 12.35m },
        { "Price|$FN_MATH_ROUND", 12m },
        { "Price|$FN_MATH_CEILING", 13m },
        { "Price|$FN_MATH_FLOOR", 12m },
        { "Price|$FN_MATH_TRUNCATE", 12m },
        { "Price|$FN_MATH_MAX(1)", 12.3456m },
        { "Quantity|$FN_MATH_MAX(10)", 10 },
        { "Quantity|$FN_MATH_MIN(Digits)", 1 },
        { "Ratio|$FN_MATH_ROUND", 0d },
        { "Quantity|$FN_MATH_ROUND", 7d },
    };

    [Theory]
    [MemberData(nameof(ExactlyTyped))]
    public void MathFunction_KeepsTheOperandType(string text, object expected)
    {
        foreach (var selector in BothForms(text))
        {
            var value = Evaluate(Body(selector));
            Assert.IsType(expected.GetType(), value);
            Assert.Equal(expected, value);
        }
    }

    [Fact]
    public void DecimalRounding_IsExact()
    {
        var item = new Product { Price = 0.5015m };

        foreach (var selector in BothForms("Price|$FN_MATH_ROUND(3)"))
        {
            var value = Expression.Lambda<Func<Product, decimal>>(Body(selector), Root).Compile()(item);

            Assert.Equal(0.502m, value);
        }

        Assert.Equal(typeof(decimal), ((PropertySelector<Product>)"Price|$FN_MATH_ROUND(3)").Type);
    }

    [Theory]
    [InlineData("Price|$FN_MATH_MAX(1)")]
    [InlineData("Price|$FN_MATH_MIN(1)")]
    public void DecimalMaxAndMin_AreDecimal(string text)
    {
        Assert.Equal(typeof(decimal), ((PropertySelector<Product>)text).Type);
    }

    [Fact]
    public void PipedDateFunction_KeepsTheSubjectType()
    {
        foreach (var selector in BothForms("Day|$FN_DT_ADDDAYS(3)"))
            Assert.Equal(new DateOnly(2024, 3, 18), Evaluate(Body(selector)));

        foreach (var selector in BothForms("Stamp|$FN_DT_ADDHOURS(1)"))
            Assert.Equal(new DateTimeOffset(2024, 3, 15, 11, 0, 0, TimeSpan.FromHours(2)), Evaluate(Body(selector)));
    }

    [Theory]
    [InlineData("Quantity|$FN_MATH_MAX(Quantity, 10)")]
    [InlineData("Ratio|$FN_MATH_SQRT(Ratio)")]
    [InlineData("Created|$FN_DT_ADDDAYS(Created, 3)")]
    [InlineData("Name|$FN_STR_ISNULLOREMPTY(Name)")]
    public void PipedForm_WithTheSubjectRepeatedAsAnArgument_IsRejected(string text)
    {
        Assert.Throws<SelectorParseException>(() => (PropertySelector)text);
        Assert.Throws<SelectorParseException>(() => (PropertySelector<Product>)text);
    }

    [Theory]
    [InlineData("Price|$FN_MATH_ROUND")]
    [InlineData("Price|$FN_MATH_ROUND(2)")]
    [InlineData("Name|$FN_STR_CONCAT(\"-\", Nickname)")]
    [InlineData("Created|$FN_DT_ADDDAYS(3)|$FN_DT_ADDHOURS(Quantity)")]
    public void ToString_KeepsThePipedForm(string text)
    {
        foreach (var selector in BothForms(text))
            Assert.Equal(text, selector.ToString());
    }

    private static PropertySelector[] BothForms(string text) =>
        [(PropertySelector)text, (PropertySelector<Product>)text];

    private static readonly ParameterExpression Root = Expression.Parameter(typeof(Product), "p");

    private static Expression Body(PropertySelector selector) => ExpressionUtils.CreateMemberAccess(Root, selector);

    private static object? Evaluate(Expression body) =>
        Expression.Lambda<Func<Product, object?>>(Expression.Convert(body, typeof(object)), Root).Compile()(Sample);
}
