using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.DateSelectors;

public class DateSelectorTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateOnly StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
    }

    private static readonly DateTime SampleDateTime = new(2024, 3, 15, 10, 30, 45, 500);

    // Walks the whole selector chain, so chained parts are covered too (a single
    // PropertyAccess call would only build the first part).
    private static TResult Evaluate<TResult>(PropertySelector<TestItem> selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static object? EvaluateBoxed(PropertySelector selector)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = Expression.Convert(ExpressionUtils.CreateMemberAccess(parameter, selector), typeof(object));
        return Expression.Lambda<Func<TestItem, object?>>(body, parameter).Compile()(new TestItem());
    }

    // ── Add functions ───────────────────────────────────────────────────────

    [Fact]
    public void DateAddDays_OnDateTimeProperty_WithIntegerAmount_AddsDays()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddDays}(CreatedAt, 5)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddDays(5), result);
    }

    [Fact]
    public void DateAddDays_OnDateTimeProperty_WithDoubleAmount_AddsFractionalDays()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddDays}(CreatedAt, 1.5)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddDays(1.5), result);
    }

    [Fact]
    public void DateAddMonths_OnDateOnlyProperty_AddsMonths()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddMonths}(StartDate, 2)";

        var result = Evaluate<DateOnly>(selector, new TestItem { StartDate = new DateOnly(2023, 7, 1) });

        Assert.Equal(new DateOnly(2023, 9, 1), result);
    }

    [Fact]
    public void DateAddYears_OnNullableDateTimeProperty_AddsYears()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddYears}(UpdatedAt, 1)";

        var result = Evaluate<DateTime>(selector, new TestItem { UpdatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddYears(1), result);
    }

    [Fact]
    public void DateAddHours_OnTimeOnlyProperty_AddsHours()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddHours}(StartTime, 2)";

        var result = Evaluate<TimeOnly>(selector, new TestItem { StartTime = new TimeOnly(18, 45) });

        Assert.Equal(new TimeOnly(20, 45), result);
    }

    [Fact]
    public void DateAddMinutes_OnDateTimeProperty_AddsMinutes()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddMinutes}(CreatedAt, 90)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddMinutes(90), result);
    }

    [Fact]
    public void DateAddMilliseconds_OnDateTimeProperty_AddsMilliseconds()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddMilliseconds}(CreatedAt, 250)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddMilliseconds(250), result);
    }

    [Fact]
    public void DateAddSeconds_OnDateTimeProperty_AddsSeconds()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddSeconds}(CreatedAt, 30)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddSeconds(30), result);
    }

    [Fact]
    public void DateAddDays_OnNonGenericSelector_AddsDays()
    {
        PropertySelector selector = $"{SelectorDefaults.DateAddDays}(CreatedAt, 5)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(SampleDateTime.AddDays(5), result);
    }

    [Fact]
    public void DateAddSeconds_OnTimeOnlyProperty_ThrowsInvalidOperationExceptionOnBuild()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddSeconds}(StartTime, 30)";

        // TimeOnly has no AddSeconds; the check happens lazily when the expression is built.
        Assert.Throws<InvalidOperationException>(() => Evaluate<TimeOnly>(selector, new TestItem()));
    }

    // ── Static values ───────────────────────────────────────────────────────

    [Fact]
    public void DateNow_WithoutParameters_ReturnsCurrentDateTime()
    {
        PropertySelector<TestItem> selector = SelectorDefaults.DateNow;

        var before = DateTime.Now;
        var result = Evaluate<DateTime>(selector, new TestItem());
        var after = DateTime.Now;

        Assert.InRange(result, before, after);
    }

    [Fact]
    public void DateUtcNow_WithoutParameters_ReturnsCurrentUtcDateTime()
    {
        PropertySelector<TestItem> selector = SelectorDefaults.DateUtcNow;

        var before = DateTime.UtcNow;
        var result = Evaluate<DateTime>(selector, new TestItem());
        var after = DateTime.UtcNow;

        Assert.InRange(result, before, after);
    }

    [Fact]
    public void DateToday_WithoutParameters_ReturnsTodayAtMidnight()
    {
        PropertySelector<TestItem> selector = SelectorDefaults.DateToday;

        var result = Evaluate<DateTime>(selector, new TestItem());

        Assert.Equal(DateTime.Today, result);
    }

    // ── Value literals ──────────────────────────────────────────────────────

    [Fact]
    public void DateTimeLiteral_AsSelector_ProducesDateTimeConstant()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateTimeLiteral}(\"2024-03-15T10:30:45\")";

        var result = Evaluate<DateTime>(selector, new TestItem());

        Assert.Equal(typeof(DateTime), selector.SelectorType);
        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 45), result);
    }

    [Fact]
    public void DateOnlyLiteral_AsSelector_ProducesDateOnlyConstant()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateOnlyLiteral}(\"2024-03-15\")";

        var result = Evaluate<DateOnly>(selector, new TestItem());

        Assert.Equal(typeof(DateOnly), selector.SelectorType);
        Assert.Equal(new DateOnly(2024, 3, 15), result);
    }

    [Fact]
    public void TimeOnlyLiteral_AsSelector_ProducesTimeOnlyConstant()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.TimeOnlyLiteral}(\"10:30:45\")";

        var result = Evaluate<TimeOnly>(selector, new TestItem());

        Assert.Equal(typeof(TimeOnly), selector.SelectorType);
        Assert.Equal(new TimeOnly(10, 30, 45), result);
    }

    public static TheoryData<string, object> DateLiterals() => new()
    {
        { "$FN_DT_DATETIME(\"2024-03-15T10:30:45\")", new DateTime(2024, 3, 15, 10, 30, 45) },
        { "$FN_DT_DATETIME(\"2024-03-15\")", new DateTime(2024, 3, 15) },
        { "$FN_DT_DATEONLY(\"2024-03-15\")", new DateOnly(2024, 3, 15) },
        { "$FN_DT_TIMEONLY(\"10:30\")", new TimeOnly(10, 30) },
        { "$FN_DT_TIMEONLY(\"10:30:45.5\")", new TimeOnly(10, 30, 45, 500) },
    };

    [Theory]
    [MemberData(nameof(DateLiterals))]
    public void DateLiteral_InBothForms_IsAConstantOfItsType(string text, object expected)
    {
        PropertySelector<TestItem> generic = text;
        PropertySelector nonGeneric = text;

        Assert.Equal(expected.GetType(), generic.Type);
        Assert.Equal(expected.GetType(), nonGeneric.Type);
        Assert.Equal(expected, EvaluateBoxed(generic));
        Assert.Equal(expected, EvaluateBoxed(nonGeneric));
        Assert.IsAssignableFrom<ConstantExpression>(nonGeneric.PropertyAccess(null!));
    }

    [Fact]
    public void DateTimeLiteral_KeepsTheKind()
    {
        PropertySelector<TestItem> utc = $"{SelectorDefaults.DateTimeLiteral}(\"2024-03-15T10:30:00Z\")";
        PropertySelector<TestItem> unspecified = $"{SelectorDefaults.DateTimeLiteral}(\"2024-03-15T10:30:00\")";

        var utcValue = Evaluate<DateTime>(utc, new TestItem());

        Assert.Equal(DateTimeKind.Utc, utcValue.Kind);
        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc), utcValue);
        Assert.Equal(DateTimeKind.Unspecified, Evaluate<DateTime>(unspecified, new TestItem()).Kind);
    }

    [Theory]
    [InlineData("$FN_DT_DATETIME(\"2024-13-01\")")]
    [InlineData("$FN_DT_DATEONLY(\"not a date\")")]
    [InlineData("$FN_DT_TIMEONLY(\"25:00\")")]
    public void DateLiteral_WithAnInvalidValue_FailsWhenParsed(string text)
    {
        Assert.Throws<ArgumentException>(() => { PropertySelector<TestItem> _ = text; });
        Assert.Throws<ArgumentException>(() => { PropertySelector _ = text; });
    }

    [Theory]
    [InlineData("$FN_DT_DATETIME(CreatedAt)", "argument 1 ('CreatedAt')")]
    [InlineData("$FN_DT_DATETIME(5)", "argument 1 ('5')")]
    public void DateLiteral_WithANonLiteralArgument_FailsWhenParsed_InBothForms(string text, string argument)
    {
        var generic = Assert.Throws<InvalidOperationException>(() => { PropertySelector<TestItem> _ = text; });
        var nonGeneric = Assert.Throws<InvalidOperationException>(() => { PropertySelector _ = text; });

        Assert.Equal($"$FN_DT_DATETIME: {argument} must be a quoted string: '{text}'.", generic.Message);
        Assert.Equal(generic.Message, nonGeneric.Message);
    }

    [Theory]
    [InlineData("$FN_DT_DATETIME")]
    [InlineData("$FN_DT_DATEONLY(\"2024-01-01\", \"2024-01-02\")")]
    public void DateLiteral_WithoutExactlyOneArgument_IsRejected(string text)
    {
        Assert.ThrowsAny<Exception>(() => { PropertySelector<TestItem> _ = text; });
        Assert.ThrowsAny<Exception>(() => { PropertySelector _ = text; });
    }

    [Fact]
    public void DateLiteral_WorksAsAFunctionArgument()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateAddDays}({SelectorDefaults.DateTimeLiteral}(\"2024-03-15\"), 3)";

        Assert.Equal(new DateTime(2024, 3, 18), Evaluate<DateTime>(selector, new TestItem()));
    }

    [Fact]
    public void DateLiteral_ToString_ReparsesToTheSameValue()
    {
        PropertySelector<TestItem> selector = $"UpdatedAt|$FN_COALESCE({SelectorDefaults.DateTimeLiteral}(\"2024-03-15T10:30:00\"))";

        PropertySelector<TestItem> reparsed = selector.ToString();

        Assert.Equal("UpdatedAt|$FN_COALESCE($FN_DT_DATETIME(\"2024-03-15T10:30:00\"))", selector.ToString());
        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0), Evaluate<DateTime>(reparsed, new TestItem()));
    }
}
