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
    public void DateTimeValueSpecifier_AsSelector_ProducesDateTimeConstant()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateTimeValueSpecifier}{SelectorDefaults.ValuePrefix}2024-03-15T10:30:45";

        var result = Evaluate<DateTime>(selector, new TestItem());

        Assert.Equal(typeof(DateTime), selector.SelectorType);
        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 45), result);
    }

    [Fact]
    public void DateOnlyValueSpecifier_AsSelector_ProducesDateOnlyConstant()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}2024-03-15";

        var result = Evaluate<DateOnly>(selector, new TestItem());

        Assert.Equal(typeof(DateOnly), selector.SelectorType);
        Assert.Equal(new DateOnly(2024, 3, 15), result);
    }

    [Fact]
    public void TimeOnlyValueSpecifier_AsSelector_ProducesTimeOnlyConstant()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.TimeOnlyValueSpecifier}{SelectorDefaults.ValuePrefix}10:30:45";

        var result = Evaluate<TimeOnly>(selector, new TestItem());

        Assert.Equal(typeof(TimeOnly), selector.SelectorType);
        Assert.Equal(new TimeOnly(10, 30, 45), result);
    }
}
