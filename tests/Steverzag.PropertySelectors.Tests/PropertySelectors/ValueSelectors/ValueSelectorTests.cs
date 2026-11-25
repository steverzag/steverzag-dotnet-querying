using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.ValueSelectors;
using System.Globalization;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.ValueSelectors;

public class ValueSelectorTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static ConstantExpression BuildConstant(PropertySelector selector)
    {
        var body = selector.PropertyAccess(null);
        return Assert.IsAssignableFrom<ConstantExpression>(body);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector)
    {
        var body = selector.PropertyAccess(null);
        return Expression.Lambda<Func<TResult>>(body).Compile()();
    }

    private static object? EvaluateBoxed(PropertySelector selector)
    {
        var body = Expression.Convert(selector.PropertyAccess(null), typeof(object));
        return Expression.Lambda<Func<object?>>(body).Compile()();
    }

    private static void InCulture(string culture, Action action)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        try { action(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    // ── Integer ─────────────────────────────────────────────────────────────

    [Fact]
    public void IntegerValueSelector_NonGeneric_ProducesIntConstant()
    {
        var selector = new IntegerValueSelector(42);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Equal(typeof(int), constant.Type);
        Assert.Equal(42, constant.Value);
        Assert.Equal(42, selector.Value);
        Assert.Equal(42, Evaluate<int>(selector));
    }

    [Fact]
    public void IntegerValueSelector_Generic_ProducesIntConstant()
    {
        var selector = new IntegerValueSelector<TestItem>(42);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Equal(typeof(int), constant.Type);
        Assert.Equal(42, Evaluate<int>(selector));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(-7, "-7")]
    [InlineData(int.MaxValue, "2147483647")]
    [InlineData(int.MinValue, "-2147483648")]
    public void IntegerValueSelector_PlainValueIsTheBareLiteral(int value, string expected)
    {
        var selector = new IntegerValueSelector(value);

        Assert.Equal(value, Evaluate<int>(selector));
        Assert.Equal(expected, selector.PlainValue);
    }

    // ── Long ────────────────────────────────────────────────────────────────

    [Fact]
    public void LongValueSelector_ProducesLongConstant_InBothForms()
    {
        Assert.Equal(typeof(long), BuildConstant(new LongValueSelector(5)).Type);
        Assert.Equal(typeof(long), BuildConstant(new LongValueSelector<TestItem>(5)).Type);
        Assert.Equal(5L, Evaluate<long>(new LongValueSelector(5)));
    }

    [Theory]
    [InlineData(5L, "5L")]
    [InlineData(-5L, "-5L")]
    [InlineData(long.MaxValue, "9223372036854775807L")]
    public void LongValueSelector_PlainValueHasTheSuffix(long value, string expected)
    {
        Assert.Equal(expected, new LongValueSelector(value).PlainValue);
    }

    // ── Double ──────────────────────────────────────────────────────────────

    [Fact]
    public void DoubleValueSelector_NonGeneric_ProducesDoubleConstant()
    {
        var selector = new DoubleValueSelector(1.5);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(double), selector.SelectorType);
        Assert.Equal(typeof(double), constant.Type);
        Assert.Equal(1.5, selector.Value);
        Assert.Equal(1.5, Evaluate<double>(selector));
    }

    [Fact]
    public void DoubleValueSelector_Generic_ProducesDoubleConstant()
    {
        var selector = new DoubleValueSelector<TestItem>(1.5);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(double), selector.SelectorType);
        Assert.Equal(typeof(double), constant.Type);
        Assert.Equal(1.5, Evaluate<double>(selector));
    }

    [Theory]
    [InlineData(1.5, "1.5")]
    [InlineData(2d, "2.0")]
    [InlineData(-2.25, "-2.25")]
    [InlineData(1e20, "1E+20")]
    public void DoubleValueSelector_PlainValueIsInvariantAndAlwaysReadsAsADouble(double value, string expected)
    {
        InCulture("es-ES", () => Assert.Equal(expected, new DoubleValueSelector(value).PlainValue));
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-2.25)]
    [InlineData(0.1)]
    [InlineData(2d)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    public void DoubleValueSelector_PlainValueRoundTripsThroughParsing(double value)
    {
        PropertySelector reparsed = new DoubleValueSelector(value).PlainValue;

        Assert.IsType<DoubleValueSelector>(reparsed);
        Assert.Equal(value, Evaluate<double>(reparsed));
    }

    // ── Decimal ─────────────────────────────────────────────────────────────

    [Fact]
    public void DecimalValueSelector_ProducesDecimalConstant_InBothForms()
    {
        Assert.Equal(typeof(decimal), BuildConstant(new DecimalValueSelector(1.5m)).Type);
        Assert.Equal(typeof(decimal), BuildConstant(new DecimalValueSelector<TestItem>(1.5m)).Type);
        Assert.Equal(1.5m, Evaluate<decimal>(new DecimalValueSelector(1.5m)));
    }

    [Fact]
    public void DecimalValueSelector_PlainValueHasTheSuffixAndKeepsTheScale()
    {
        InCulture("es-ES", () =>
        {
            Assert.Equal("1.50m", new DecimalValueSelector(1.50m).PlainValue);
            Assert.Equal("-3m", new DecimalValueSelector(-3m).PlainValue);
        });
    }

    // ── Boolean ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanValueSelector_NonGeneric_ProducesBoolConstant(bool value)
    {
        var selector = new BooleanValueSelector(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(bool), selector.SelectorType);
        Assert.Equal(typeof(bool), constant.Type);
        Assert.Equal(value, selector.Value);
        Assert.Equal(value, Evaluate<bool>(selector));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanValueSelector_Generic_ProducesBoolConstant(bool value)
    {
        var selector = new BooleanValueSelector<TestItem>(value);

        BuildConstant(selector);

        Assert.Equal(typeof(bool), selector.SelectorType);
        Assert.Equal(value, Evaluate<bool>(selector));
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void BooleanValueSelector_PlainValueIsLowercase(bool value, string expected)
    {
        Assert.Equal(expected, new BooleanValueSelector(value).PlainValue);
    }

    // ── String ──────────────────────────────────────────────────────────────

    [Fact]
    public void StringValueSelector_NonGeneric_ProducesStringConstant()
    {
        var selector = new StringValueSelector("hello");

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(string), selector.SelectorType);
        Assert.Equal(typeof(string), constant.Type);
        Assert.Equal("hello", selector.Value);
        Assert.Equal("hello", Evaluate<string>(selector));
    }

    [Fact]
    public void StringValueSelector_Generic_ProducesStringConstant()
    {
        var selector = new StringValueSelector<TestItem>("hello");

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(string), selector.SelectorType);
        Assert.Equal(typeof(string), constant.Type);
        Assert.Equal("hello", Evaluate<string>(selector));
    }

    [Theory]
    [InlineData("hello", "\"hello\"")]
    [InlineData("", "\"\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("a|b, (c)", "\"a|b, (c)\"")]
    public void StringValueSelector_PlainValueIsQuotedWithDoubledQuotes(string value, string expected)
    {
        var selector = new StringValueSelector(value);

        Assert.Equal(expected, selector.PlainValue);
        Assert.Equal(expected, selector.ToString());
    }

    // ── Shared behaviour ────────────────────────────────────────────────────

    [Fact]
    public void ValueSelectors_IgnoreTheTargetExpression()
    {
        var selector = new IntegerValueSelector<TestItem>(7);
        var parameter = Expression.Parameter(typeof(TestItem), "x");

        var withTarget = selector.PropertyAccess(parameter);
        var withoutTarget = selector.PropertyAccess(null);

        var lambda = Expression.Lambda<Func<TestItem, int>>(withTarget, parameter).Compile();

        Assert.Equal(7, lambda(new TestItem { Id = 99 }));
        Assert.Equal(((ConstantExpression)withTarget).Value, ((ConstantExpression)withoutTarget).Value);
    }

    [Fact]
    public void GenericValueSelector_DoesNotResolveTheValueAsAPropertyOfT()
    {
        var selector = new IntegerValueSelector<TestItem>(42);

        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Null(selector.ChildSelector);
        Assert.Equal(typeof(int), selector.Type);
    }

    [Fact]
    public void ValueSelectors_WithSameValue_AreNotRecordEqual()
    {
        // Record equality includes the PropertyAccess closure, which is new per instance.
        var first = new IntegerValueSelector(42);
        var second = new IntegerValueSelector(42);

        Assert.NotEqual(first, second);
        Assert.Equal(first.PlainValue, second.PlainValue);
        Assert.Equal(first, first);
    }

    // ── Parsing: bare literals ──────────────────────────────────────────────

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("0", typeof(int), 0)]
    [InlineData("2147483647", typeof(int), int.MaxValue)]
    [InlineData("2147483648", typeof(long), 2147483648L)]
    [InlineData("5L", typeof(long), 5L)]
    [InlineData("5l", typeof(long), 5L)]
    [InlineData("1.5", typeof(double), 1.5)]
    [InlineData("2.0", typeof(double), 2.0)]
    [InlineData("1e3", typeof(double), 1000.0)]
    [InlineData("1E+20", typeof(double), 1e20)]
    [InlineData("2.5e-3", typeof(double), 0.0025)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("True", typeof(bool), true)]
    [InlineData("FALSE", typeof(bool), false)]
    public void Parse_BareLiteral_InBothForms(string text, Type expectedType, object expected)
    {
        PropertySelector<TestItem> generic = text;
        PropertySelector nonGeneric = text;

        Assert.Equal(expectedType, generic.SelectorType);
        Assert.Equal(expectedType, nonGeneric.SelectorType);
        Assert.Equal(expected, EvaluateBoxed(generic));
        Assert.Equal(expected, EvaluateBoxed(nonGeneric));
    }

    [Theory]
    [InlineData("1.5m", "1.5")]
    [InlineData("1.50M", "1.50")]
    [InlineData("3m", "3")]
    [InlineData("79228162514264337593543950335m", "79228162514264337593543950335")]
    public void Parse_DecimalLiteral_IsExact(string text, string expected)
    {
        PropertySelector<TestItem> selector = text;

        Assert.Equal(typeof(decimal), selector.SelectorType);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Evaluate<decimal>(selector));
    }

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("1.234", 1.234)]
    public void Parse_BareDouble_IsReadWithTheInvariantCulture(string text, double expected)
    {
        // es-ES uses '.' as the group separator, so a culture-sensitive parse would read 1.5 as 15.
        InCulture("es-ES", () =>
        {
            PropertySelector<TestItem> selector = text;
            Assert.Equal(expected, Evaluate<double>(selector));
        });
    }

    [Theory]
    [InlineData("99999999999999999999")]
    [InlineData("99999999999999999999L")]
    [InlineData("1.5L")]
    [InlineData("1e400")]
    public void Parse_NumberThatDoesNotFit_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => { PropertySelector<TestItem> _ = text; });
        Assert.Throws<ArgumentException>(() => { PropertySelector _ = text; });
    }

    [Theory]
    [InlineData("-7", -7)]
    [InlineData("-2147483648", int.MinValue)]
    public void Parse_NegativeNumber_IsALiteral_NotANegation(string text, int expected)
    {
        PropertySelector<TestItem> generic = text;
        PropertySelector nonGeneric = text;

        Assert.IsType<IntegerValueSelector<TestItem>>(generic);
        Assert.IsType<IntegerValueSelector>(nonGeneric);
        Assert.Equal(expected, Evaluate<int>(generic));
    }

    [Theory]
    [InlineData("-1.5", typeof(double))]
    [InlineData("-5L", typeof(long))]
    [InlineData("-2.5m", typeof(decimal))]
    public void Parse_NegativeNumber_KeepsItsType(string text, Type expectedType)
    {
        PropertySelector<TestItem> selector = text;

        Assert.Equal(expectedType, selector.SelectorType);
    }

    // ── Parsing: strings ────────────────────────────────────────────────────

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"\"", "")]
    [InlineData("\"say \"\"hi\"\"\"", "say \"hi\"")]
    [InlineData("\"\"\"\"", "\"")]
    [InlineData("\"a-b.c:d\"", "a-b.c:d")]
    public void Parse_QuotedLiteral_UndoublesQuotes(string text, string expected)
    {
        PropertySelector<TestItem> generic = text;
        PropertySelector nonGeneric = text;

        Assert.IsType<StringValueSelector<TestItem>>(generic);
        Assert.Equal(expected, Evaluate<string>(generic));
        Assert.Equal(expected, Evaluate<string>(nonGeneric));
    }

    [Fact]
    public void Parse_QuotedLiteralWithASingleInnerQuote_Throws()
    {
        Assert.Throws<ArgumentException>(() => { PropertySelector<TestItem> _ = "\"a\" \"b\""; });
    }

    [Fact]
    public void Parse_LoneQuoteCharacter_IsTreatedAsAPropertyPath()
    {
        // A single quote starts and ends with '"' at the same index, so it is not an empty literal.
        PropertySelector selector = "\"";

        Assert.IsType<PropertySelector>(selector);
        Assert.Equal("\"", selector.PlainValue);
    }

    [Fact]
    public void Parse_NonLiteralPart_FallsBackToAPropertyPathSelector()
    {
        PropertySelector<TestItem> selector = "Name";

        Assert.IsType<PropertySelector<TestItem>>(selector);
        Assert.Equal(typeof(string), selector.SelectorType);
    }

    [Theory]
    [InlineData("$VAL_INT:5")]
    [InlineData("$VAL_STR:hello")]
    public void Parse_RemovedValueSpecifier_IsNoLongerALiteral(string text)
    {
        PropertySelector selector = text;

        Assert.IsType<PropertySelector>(selector);
        Assert.Throws<InvalidOperationException>(() => { PropertySelector<TestItem> _ = text; });
    }

    // ── Plain values reparse ────────────────────────────────────────────────

    [Fact]
    public void PlainValue_RoundTripsThroughParsing_ForEveryScalarType()
    {
        PropertySelector[] selectors =
        [
            new IntegerValueSelector(-7),
            new IntegerValueSelector(int.MinValue),
            new LongValueSelector(long.MinValue),
            new DoubleValueSelector(-0.1),
            new DecimalValueSelector(-12.340m),
            new BooleanValueSelector(false),
            new StringValueSelector("say \"hi\", (a|b)"),
        ];

        foreach (var original in selectors)
        {
            PropertySelector reparsed = original.PlainValue;

            Assert.Equal(original.GetType(), reparsed.GetType());
            Assert.Equal(EvaluateBoxed(original), EvaluateBoxed(reparsed));
            Assert.Equal(original.PlainValue, reparsed.PlainValue);
        }
    }

    // ── Public formatter ────────────────────────────────────────────────────

    public static TheoryData<object, string> FormattedValues() => new()
    {
        { 5, "5" },
        { (short)-3, "-3" },
        { 5L, "5L" },
        { 1.5, "1.5" },
        { 3.0, "3.0" },
        { 1.5f, "1.5" },
        { 2.50m, "2.50m" },
        { true, "true" },
        { "it's \"x\"", "\"it's \"\"x\"\"\"" },
        { new DateTime(2024, 1, 15, 8, 30, 0), "$FN_DT_DATETIME(\"2024-01-15T08:30:00.0000000\")" },
        { new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Utc), "$FN_DT_DATETIME(\"2024-01-15T08:30:00.0000000Z\")" },
        { new DateOnly(2024, 1, 15), "$FN_DT_DATEONLY(\"2024-01-15\")" },
        { new TimeOnly(8, 30, 15), "$FN_DT_TIMEONLY(\"08:30:15.0000000\")" },
    };

    [Theory]
    [MemberData(nameof(FormattedValues))]
    public void SelectorLiteral_Format_WritesTheLiteral(object value, string expected)
    {
        InCulture("es-ES", () => Assert.Equal(expected, SelectorLiteral.Format(value)));
    }

    [Theory]
    [MemberData(nameof(FormattedValues))]
    public void SelectorLiteral_Format_ParsesBackToTheSameValue(object value, string text)
    {
        PropertySelector<TestItem> generic = text;
        PropertySelector nonGeneric = text;

        var expected = value switch
        {
            short s => (object)(int)s,
            float f => (double)f,
            _ => value,
        };

        Assert.Equal(expected, EvaluateBoxed(generic));
        Assert.Equal(expected, EvaluateBoxed(nonGeneric));
    }

    [Fact]
    public void SelectorLiteral_Format_RejectsValuesWithoutALiteral()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectorLiteral.Format(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectorLiteral.Format(double.PositiveInfinity));
        Assert.Throws<NotSupportedException>(() => SelectorLiteral.Format(Guid.Empty));
        Assert.Throws<ArgumentNullException>(() => SelectorLiteral.Format((object)null!));
    }
}
