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

    /// <summary>
    /// Value selectors ignore the target expression entirely, so a null target is enough
    /// to build their body. Also asserts the body really is a constant.
    /// </summary>
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
    [InlineData(0)]
    [InlineData(-7)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void IntegerValueSelector_WithEdgeValues_KeepsValue(int value)
    {
        var selector = new IntegerValueSelector(value);

        Assert.Equal(value, Evaluate<int>(selector));
        Assert.Equal($"{SelectorDefaults.IntegerValueSpecifier}{SelectorDefaults.ValuePrefix}{value}", selector.PlainValue);
    }

    [Fact]
    public void IntegerValueSelector_SetsPlainValueWithSpecifierAndPrefix()
    {
        var selector = new IntegerValueSelector(42);

        Assert.Equal($"{SelectorDefaults.IntegerValueSpecifier}{SelectorDefaults.ValuePrefix}42", selector.PlainValue);
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
    [InlineData(0d)]
    [InlineData(-2.25d)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    public void DoubleValueSelector_WithEdgeValues_KeepsValue(double value)
    {
        var selector = new DoubleValueSelector(value);

        Assert.Equal(value, Evaluate<double>(selector));
    }

    [Fact]
    public void DoubleValueSelector_SetsPlainValueUsingInvariantCulture()
    {
        var selector = new DoubleValueSelector(1.5);

        // Invariant so the emitted plain value parses back the same way on any machine.
        Assert.Equal($"{SelectorDefaults.DoubleValueSpecifier}{SelectorDefaults.ValuePrefix}1.5", selector.PlainValue);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-2.25)]
    [InlineData(0.1)]
    [InlineData(double.MaxValue)]
    public void DoubleValueSelector_PlainValueRoundTripsThroughParsing(double value)
    {
        var selector = new DoubleValueSelector(value);

        PropertySelector reparsed = selector.PlainValue;

        Assert.Equal(value, Evaluate<double>(reparsed));
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

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(bool), selector.SelectorType);
        Assert.Equal(value, Evaluate<bool>(selector));
    }

    [Fact]
    public void BooleanValueSelector_SetsPlainValueWithSpecifierAndPrefix()
    {
        var selector = new BooleanValueSelector(true);

        Assert.Equal($"{SelectorDefaults.BooleanValueSpecifier}{SelectorDefaults.ValuePrefix}True", selector.PlainValue);
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

    [Fact]
    public void StringValueSelector_WithEmptyValue_ProducesEmptyConstant()
    {
        var selector = new StringValueSelector(string.Empty);

        Assert.Equal(string.Empty, Evaluate<string>(selector));
        Assert.Equal($"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}", selector.PlainValue);
    }

    [Fact]
    public void StringValueSelector_WithNullValue_KeepsStringTypedConstant()
    {
        // Implementations.CreateStringConstantAccesor passes typeof(string) explicitly so a
        // null value still yields a string-typed constant instead of an untyped null.
        var selector = new StringValueSelector(null!);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(string), constant.Type);
        Assert.Null(constant.Value);
        Assert.Null(Evaluate<string>(selector));
    }

    [Fact]
    public void StringValueSelector_SetsPlainValueWithSpecifierAndPrefix()
    {
        var selector = new StringValueSelector("hello");

        Assert.Equal($"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}hello", selector.PlainValue);
    }

    // ── DateTime ────────────────────────────────────────────────────────────

    [Fact]
    public void DateTimeValueSelector_NonGeneric_ProducesDateTimeConstant()
    {
        var value = new DateTime(2024, 1, 15, 10, 30, 0);
        var selector = new DateTimeValueSelector(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(DateTime), selector.SelectorType);
        Assert.Equal(typeof(DateTime), constant.Type);
        Assert.Equal(value, selector.Value);
        Assert.Equal(value, Evaluate<DateTime>(selector));
    }

    [Fact]
    public void DateTimeValueSelector_Generic_ProducesDateTimeConstant()
    {
        var value = new DateTime(2024, 1, 15, 10, 30, 0);
        var selector = new DateTimeValueSelector<TestItem>(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(DateTime), selector.SelectorType);
        Assert.Equal(value, Evaluate<DateTime>(selector));
    }

    [Fact]
    public void DateTimeValueSelector_SetsPlainValueWithRoundTripFormat()
    {
        var value = new DateTime(2024, 1, 15, 10, 30, 0);
        var selector = new DateTimeValueSelector(value);

        Assert.Equal(
            $"{SelectorDefaults.DateTimeValueSpecifier}{SelectorDefaults.ValuePrefix}2024-01-15T10:30:00.0000000",
            selector.PlainValue);
    }

    [Fact]
    public void DateTimeValueSelector_WithUtcKind_KeepsKindInPlainValueAndConstant()
    {
        var value = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var selector = new DateTimeValueSelector(value);

        Assert.EndsWith("2024-01-15T10:30:00.0000000Z", selector.PlainValue);
        Assert.Equal(DateTimeKind.Utc, Evaluate<DateTime>(selector).Kind);
    }

    [Fact]
    public void DateTimeValueSelector_PlainValueIsInvariantAndRoundTrips()
    {
        var value = new DateTime(2024, 12, 31, 23, 59, 59, 999);
        var selector = new DateTimeValueSelector(value);

        var payload = selector.PlainValue[(SelectorDefaults.DateTimeValueSpecifier.Length + SelectorDefaults.ValuePrefix.Length)..];

        Assert.Equal(value, DateTime.Parse(payload, CultureInfo.InvariantCulture));
    }

    // ── DateOnly ────────────────────────────────────────────────────────────

    [Fact]
    public void DateOnlyValueSelector_NonGeneric_ProducesDateOnlyConstant()
    {
        var value = new DateOnly(2024, 1, 15);
        var selector = new DateOnlyValueSelector(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(DateOnly), selector.SelectorType);
        Assert.Equal(typeof(DateOnly), constant.Type);
        Assert.Equal(value, selector.Value);
        Assert.Equal(value, Evaluate<DateOnly>(selector));
    }

    [Fact]
    public void DateOnlyValueSelector_Generic_ProducesDateOnlyConstant()
    {
        var value = new DateOnly(2024, 1, 15);
        var selector = new DateOnlyValueSelector<TestItem>(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(DateOnly), selector.SelectorType);
        Assert.Equal(value, Evaluate<DateOnly>(selector));
    }

    [Fact]
    public void DateOnlyValueSelector_SetsPlainValueWithRoundTripFormat()
    {
        var selector = new DateOnlyValueSelector(new DateOnly(2024, 1, 15));

        Assert.Equal(
            $"{SelectorDefaults.DateOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}2024-01-15",
            selector.PlainValue);
    }

    [Fact]
    public void DateOnlyValueSelector_PlainValueIsInvariantAndRoundTrips()
    {
        var value = new DateOnly(2024, 12, 31);
        var selector = new DateOnlyValueSelector(value);

        var payload = selector.PlainValue[(SelectorDefaults.DateOnlyValueSpecifier.Length + SelectorDefaults.ValuePrefix.Length)..];

        Assert.Equal(value, DateOnly.Parse(payload, CultureInfo.InvariantCulture));
    }

    // ── TimeOnly ────────────────────────────────────────────────────────────

    [Fact]
    public void TimeOnlyValueSelector_NonGeneric_ProducesTimeOnlyConstant()
    {
        var value = new TimeOnly(10, 30, 15);
        var selector = new TimeOnlyValueSelector(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(TimeOnly), selector.SelectorType);
        Assert.Equal(typeof(TimeOnly), constant.Type);
        Assert.Equal(value, selector.Value);
        Assert.Equal(value, Evaluate<TimeOnly>(selector));
    }

    [Fact]
    public void TimeOnlyValueSelector_Generic_ProducesTimeOnlyConstant()
    {
        var value = new TimeOnly(10, 30, 15);
        var selector = new TimeOnlyValueSelector<TestItem>(value);

        var constant = BuildConstant(selector);

        Assert.Equal(typeof(TimeOnly), selector.SelectorType);
        Assert.Equal(value, Evaluate<TimeOnly>(selector));
    }

    [Fact]
    public void TimeOnlyValueSelector_SetsPlainValueWithRoundTripFormat()
    {
        var selector = new TimeOnlyValueSelector(new TimeOnly(10, 30, 15));

        Assert.Equal(
            $"{SelectorDefaults.TimeOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}10:30:15.0000000",
            selector.PlainValue);
    }

    [Fact]
    public void TimeOnlyValueSelector_PlainValueIsInvariantAndRoundTrips()
    {
        var value = new TimeOnly(23, 59, 59, 999);
        var selector = new TimeOnlyValueSelector(value);

        var payload = selector.PlainValue[(SelectorDefaults.TimeOnlyValueSpecifier.Length + SelectorDefaults.ValuePrefix.Length)..];

        Assert.Equal(value, TimeOnly.Parse(payload, CultureInfo.InvariantCulture));
    }

    // ── Shared behaviour ────────────────────────────────────────────────────

    [Fact]
    public void ValueSelectors_IgnoreTheTargetExpression()
    {
        var selector = new IntegerValueSelector<TestItem>(7);
        var parameter = Expression.Parameter(typeof(TestItem), "x");

        var withTarget = selector.PropertyAccess(parameter);
        var withoutTarget = selector.PropertyAccess(null);

        // Both bodies are the same constant, so the lambda never touches the parameter.
        var lambda = Expression.Lambda<Func<TestItem, int>>(withTarget, parameter).Compile();

        Assert.Equal(7, lambda(new TestItem { Id = 99 }));
        Assert.Equal(((ConstantExpression)withTarget).Value, ((ConstantExpression)withoutTarget).Value);
    }

    [Fact]
    public void GenericValueSelector_DoesNotResolveTheValueAsAPropertyOfT()
    {
        // SelectorType is initialized by the record before the base constructor runs, so the
        // base never tries to resolve the literal as a member of TestItem.
        var selector = new IntegerValueSelector<TestItem>(42);

        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Null(selector.ChildSelector);
        Assert.Equal(typeof(int), selector.Type);
    }

    [Fact]
    public void ValueSelectors_ToString_ContainsThePlainValue()
    {
        var selector = new StringValueSelector("hello");

        // Value selectors are records, so the compiler-synthesized ToString wins over
        // PropertySelector.ToString(): the output carries the plain value inside the record
        // dump instead of being the bare selector string. PlainValue stays the canonical form.
        Assert.Contains(selector.PlainValue, selector.ToString());
        Assert.Equal($"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}hello", selector.PlainValue);
    }

    [Fact]
    public void ValueSelectors_WithSameValue_AreNotRecordEqual()
    {
        // The synthesized record equality includes the PropertyAccess delegate, and every
        // instance builds a fresh closure, so two selectors of the same value never compare
        // equal. Compare PlainValue when identity of the selector is what matters.
        var first = new IntegerValueSelector(42);
        var second = new IntegerValueSelector(42);

        Assert.NotEqual(first, second);
        Assert.Equal(first.PlainValue, second.PlainValue);
        Assert.Equal(first, first);
    }

    // ── Parsing: literals reaching the value selectors ──────────────────────

    [Fact]
    public void Parse_BareIntegerLiteral_CreatesIntegerValueSelector()
    {
        PropertySelector<TestItem> selector = "42";

        Assert.IsType<IntegerValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Equal(42, Evaluate<int>(selector));
    }

    [Fact]
    public void Parse_BareDoubleLiteral_CreatesDoubleValueSelector()
    {
        // Bare numeric literals are parsed with the current culture, so the literal is
        // formatted the same way to keep the test culture independent.
        PropertySelector<TestItem> selector = 1.5.ToString(CultureInfo.CurrentCulture);

        Assert.IsType<DoubleValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(double), selector.SelectorType);
        Assert.Equal(1.5, Evaluate<double>(selector));
    }

    [Fact]
    public void Parse_BareBooleanLiteral_CreatesBooleanValueSelector()
    {
        PropertySelector<TestItem> selector = "true";

        Assert.IsType<BooleanValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(bool), selector.SelectorType);
        Assert.True(Evaluate<bool>(selector));
    }

    [Fact]
    public void Parse_QuotedLiteral_CreatesStringValueSelectorWithoutQuotes()
    {
        PropertySelector<TestItem> selector = "\"hello\"";

        Assert.IsType<StringValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(string), selector.SelectorType);
        Assert.Equal("hello", Evaluate<string>(selector));
    }

    [Fact]
    public void Parse_DateTimeValueSpecifier_CreatesDateTimeValueSelector()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.DateTimeValueSpecifier}{SelectorDefaults.ValuePrefix}2024-01-15T10:30:00";

        Assert.IsType<DateTimeValueSelector<TestItem>>(selector);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), Evaluate<DateTime>(selector));
    }

    [Fact]
    public void Parse_DateOnlyValueSpecifier_CreatesDateOnlyValueSelector()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.DateOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}2024-01-15";

        Assert.IsType<DateOnlyValueSelector<TestItem>>(selector);
        Assert.Equal(new DateOnly(2024, 1, 15), Evaluate<DateOnly>(selector));
    }

    [Fact]
    public void Parse_TimeOnlyValueSpecifier_CreatesTimeOnlyValueSelector()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.TimeOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}10:30:15";

        Assert.IsType<TimeOnlyValueSelector<TestItem>>(selector);
        Assert.Equal(new TimeOnly(10, 30, 15), Evaluate<TimeOnly>(selector));
    }

    [Fact]
    public void Parse_NonGeneric_DateOnlyValueSpecifier_CreatesDateOnlyValueSelector()
    {
        PropertySelector selector =
            $"{SelectorDefaults.DateOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}2024-01-15";

        Assert.IsType<DateOnlyValueSelector>(selector);
        Assert.Equal(new DateOnly(2024, 1, 15), Evaluate<DateOnly>(selector));
    }

    [Fact]
    public void Parse_StringValueSpecifier_CreatesStringValueSelector()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}hello";

        Assert.IsType<StringValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(string), selector.SelectorType);
        Assert.Equal("hello", Evaluate<string>(selector));
    }

    [Fact]
    public void Parse_StringValueSpecifier_WithEmptyPayload_CreatesEmptyStringSelector()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}";

        Assert.IsType<StringValueSelector<TestItem>>(selector);
        Assert.Equal(string.Empty, Evaluate<string>(selector));
    }

    [Fact]
    public void Parse_StringValueSpecifier_KeepsPayloadVerbatim()
    {
        // The payload is opaque: characters that are structural elsewhere (dashes, colons,
        // dots) must survive untouched instead of being read as arithmetic or member access.
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}a-b.c:d";

        Assert.Equal("a-b.c:d", Evaluate<string>(selector));
        Assert.Null(selector.ChildSelector);
    }

    [Fact]
    public void Parse_IntegerValueSpecifier_CreatesIntegerValueSelector()
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.IntegerValueSpecifier}{SelectorDefaults.ValuePrefix}42";

        Assert.IsType<IntegerValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Equal(42, Evaluate<int>(selector));
    }

    [Fact]
    public void Parse_IntegerValueSpecifier_WithNegativePayload_CreatesIntegerValueSelector()
    {
        // The leading dash would look like subtraction on a bare literal; the specifier
        // makes the payload opaque so it stays a negative constant.
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.IntegerValueSpecifier}{SelectorDefaults.ValuePrefix}-7";

        Assert.IsType<IntegerValueSelector<TestItem>>(selector);
        Assert.Equal(-7, Evaluate<int>(selector));
    }

    [Fact]
    public void Parse_DoubleValueSpecifier_CreatesDoubleValueSelector()
    {
        // The payload is read with the invariant culture, so the decimal point is fixed.
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.DoubleValueSpecifier}{SelectorDefaults.ValuePrefix}1.5";

        Assert.IsType<DoubleValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(double), selector.SelectorType);
        Assert.Equal(1.5, Evaluate<double>(selector));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    public void Parse_BooleanValueSpecifier_CreatesBooleanValueSelector(string payload, bool expected)
    {
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.BooleanValueSpecifier}{SelectorDefaults.ValuePrefix}{payload}";

        Assert.IsType<BooleanValueSelector<TestItem>>(selector);
        Assert.Equal(typeof(bool), selector.SelectorType);
        Assert.Equal(expected, Evaluate<bool>(selector));
    }

    [Fact]
    public void Parse_NonGeneric_ValueSpecifiers_CreateMatchingValueSelectors()
    {
        PropertySelector stringSelector = $"{SelectorDefaults.StringValueSpecifier}{SelectorDefaults.ValuePrefix}hello";
        PropertySelector integerSelector = $"{SelectorDefaults.IntegerValueSpecifier}{SelectorDefaults.ValuePrefix}42";
        PropertySelector doubleSelector = $"{SelectorDefaults.DoubleValueSpecifier}{SelectorDefaults.ValuePrefix}1.5";
        PropertySelector booleanSelector = $"{SelectorDefaults.BooleanValueSpecifier}{SelectorDefaults.ValuePrefix}true";

        Assert.Equal("hello", Evaluate<string>(Assert.IsType<StringValueSelector>(stringSelector)));
        Assert.Equal(42, Evaluate<int>(Assert.IsType<IntegerValueSelector>(integerSelector)));
        Assert.Equal(1.5, Evaluate<double>(Assert.IsType<DoubleValueSelector>(doubleSelector)));
        Assert.True(Evaluate<bool>(Assert.IsType<BooleanValueSelector>(booleanSelector)));
    }

    [Theory]
    [InlineData(42)]
    [InlineData(-7)]
    [InlineData(int.MinValue)]
    public void PlainValue_RoundTripsThroughParsing_ForIntegers(int value)
    {
        var selector = new IntegerValueSelector(value);

        PropertySelector reparsed = selector.PlainValue;

        Assert.Equal(value, Evaluate<int>(reparsed));
    }

    [Fact]
    public void PlainValue_RoundTripsThroughParsing_ForTheRemainingValueTypes()
    {
        PropertySelector reparsedString = new StringValueSelector("hello").PlainValue;
        PropertySelector reparsedBoolean = new BooleanValueSelector(false).PlainValue;
        PropertySelector reparsedDateTime = new DateTimeValueSelector(new DateTime(2024, 1, 15, 10, 30, 0)).PlainValue;
        PropertySelector reparsedDateOnly = new DateOnlyValueSelector(new DateOnly(2024, 1, 15)).PlainValue;
        PropertySelector reparsedTimeOnly = new TimeOnlyValueSelector(new TimeOnly(10, 30, 15)).PlainValue;

        Assert.Equal("hello", Evaluate<string>(reparsedString));
        Assert.False(Evaluate<bool>(reparsedBoolean));
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), Evaluate<DateTime>(reparsedDateTime));
        Assert.Equal(new DateOnly(2024, 1, 15), Evaluate<DateOnly>(reparsedDateOnly));
        Assert.Equal(new TimeOnly(10, 30, 15), Evaluate<TimeOnly>(reparsedTimeOnly));
    }

    [Fact]
    public void Parse_NonLiteralPart_FallsBackToAPropertyPathSelector()
    {
        PropertySelector<TestItem> selector = "Name";

        // Nothing in the value factory claims a plain property path.
        Assert.IsType<PropertySelector<TestItem>>(selector);
        Assert.Equal(typeof(string), selector.SelectorType);
    }

    [Fact]
    public void Parse_LoneQuoteCharacter_IsTreatedAsAPropertyPath()
    {
        // A single quote starts and ends with '"' at the same index, so it is not an
        // empty quoted literal and must not be sliced as one.
        PropertySelector selector = "\"";

        Assert.IsType<PropertySelector>(selector);
        Assert.Equal("\"", selector.PlainValue);
    }

    [Fact]
    public void Parse_EmptyQuotedLiteral_CreatesEmptyStringSelector()
    {
        PropertySelector<TestItem> selector = "\"\"";

        Assert.IsType<StringValueSelector<TestItem>>(selector);
        Assert.Equal(string.Empty, Evaluate<string>(selector));
    }

    [Fact]
    public void Parse_SpecifierWithoutValuePrefix_IsNotTreatedAsALiteral()
    {
        // The value prefix is what separates $VAL_DATE from $VAL_DATETIME, so a specifier
        // without it is not a literal.
        PropertySelector selector = SelectorDefaults.DateOnlyValueSpecifier;

        Assert.IsType<PropertySelector>(selector);
        Assert.Equal(SelectorDefaults.DateOnlyValueSpecifier, selector.PlainValue);
    }

    [Fact]
    public void Parse_ValueSpecifier_IsNotSplitOnArithmeticCharacters()
    {
        // Value payloads are opaque: the dashes in the date must not be read as subtraction.
        PropertySelector<TestItem> selector =
            $"{SelectorDefaults.DateOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}2024-01-15";

        Assert.IsType<DateOnlyValueSelector<TestItem>>(selector);
        Assert.Null(selector.ChildSelector);
    }
}
