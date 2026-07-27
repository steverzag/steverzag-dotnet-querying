using ExpDataWorker.PropertySelectors;
using System.Linq.Expressions;

namespace ExpDataWorker.Tests.PropertySelectors.DateSelectors;

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

    private static TResult Evaluate<TResult>(PropertySelector<TestItem> selector, TestItem item)
    {
        var parameter = Expression.Parameter(typeof(TestItem), "x");
        var body = selector.PropertyAccess(parameter);
        var lambda = Expression.Lambda<Func<TestItem, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    // ── Components ──────────────────────────────────────────────────────────

    [Fact]
    public void DateYear_OnDateTimeProperty_ReturnsYear()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateYear}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(typeof(int), selector.SelectorType);
        Assert.Equal(2024, result);
    }

    [Fact]
    public void DateMonth_OnDateTimeProperty_ReturnsMonth()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateMonth}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(3, result);
    }

    [Fact]
    public void DateDay_OnDateTimeProperty_ReturnsDay()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateDay}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(15, result);
    }

    [Fact]
    public void DateDayOfYear_OnDateTimeProperty_ReturnsDayOfYear()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateDayOfYear}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(75, result);
    }

    [Fact]
    public void DateHour_OnDateTimeProperty_ReturnsHour()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateHour}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(10, result);
    }

    [Fact]
    public void DateMinute_OnDateTimeProperty_ReturnsMinute()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateMinute}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(30, result);
    }

    [Fact]
    public void DateSecond_OnDateTimeProperty_ReturnsSecond()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateSecond}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(45, result);
    }

    [Fact]
    public void DateMillisecond_OnDateTimeProperty_ReturnsMillisecond()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateMillisecond}(CreatedAt)";

        var result = Evaluate<int>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(500, result);
    }

    [Fact]
    public void DateYear_OnNullableDateTimeProperty_UnwrapsAndReturnsYear()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateYear}(UpdatedAt)";

        var result = Evaluate<int>(selector, new TestItem { UpdatedAt = SampleDateTime });

        Assert.Equal(2024, result);
    }

    [Fact]
    public void DateYear_OnDateOnlyProperty_ReturnsYear()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateYear}(StartDate)";

        var result = Evaluate<int>(selector, new TestItem { StartDate = new DateOnly(2023, 7, 1) });

        Assert.Equal(2023, result);
    }

    [Fact]
    public void DateHour_OnTimeOnlyProperty_ReturnsHour()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateHour}(StartTime)";

        var result = Evaluate<int>(selector, new TestItem { StartTime = new TimeOnly(18, 45) });

        Assert.Equal(18, result);
    }

    [Fact]
    public void DateDatePart_OnDateTimeProperty_ReturnsDateAtMidnight()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateDatePart}(CreatedAt)";

        var result = Evaluate<DateTime>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(new DateTime(2024, 3, 15), result);
    }

    [Fact]
    public void DateTimeOfDay_OnDateTimeProperty_ReturnsTimeSpan()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateTimeOfDay}(CreatedAt)";

        var result = Evaluate<TimeSpan>(selector, new TestItem { CreatedAt = SampleDateTime });

        Assert.Equal(new TimeSpan(0, 10, 30, 45, 500), result);
    }

    [Fact]
    public void DateHour_OnDateOnlyProperty_ThrowsInvalidOperationExceptionOnBuild()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateHour}(StartDate)";

        // DateOnly has no Hour component; the check happens lazily when the expression is built.
        Assert.Throws<InvalidOperationException>(() => Evaluate<int>(selector, new TestItem()));
    }

    [Fact]
    public void DateYear_OnNonDateProperty_ThrowsInvalidOperationExceptionOnBuild()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateYear}(Id)";

        Assert.Throws<InvalidOperationException>(() => Evaluate<int>(selector, new TestItem { Id = 1 }));
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

    [Fact]
    public void DateYear_WithDateTimeLiteralParameter_ReturnsYearOfLiteral()
    {
        PropertySelector<TestItem> selector = $"{SelectorDefaults.DateYear}({SelectorDefaults.DateTimeValueSpecifier}{SelectorDefaults.ValuePrefix}2022-01-01T00:00:00)";

        var result = Evaluate<int>(selector, new TestItem());

        Assert.Equal(2022, result);
    }
}
