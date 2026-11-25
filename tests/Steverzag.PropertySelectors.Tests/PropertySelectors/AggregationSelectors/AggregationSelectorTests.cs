using Steverzag.PropertySelectors;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.Tests.AggregationSelectors;

/// <summary>
/// Covers the aggregation selector family end to end: the string is parsed into a selector
/// chain, the chain is turned into an expression and the expression is executed.
/// </summary>
public class AggregationSelectorTests
{
    private class TestLine
    {
        public int Amount { get; set; }
        public double Weight { get; set; }
    }

    private class TestOrder
    {
        public List<TestLine> Lines { get; set; } = [];
        public List<int> Numbers { get; set; } = [];
        public List<double> Weights { get; set; } = [];
        public string Code { get; set; } = string.Empty;

        // one list per element type, all holding 1, 2, 4 (the nullable ones also hold a null)
        public List<long> Longs { get; set; } = [1, 2, 4];
        public List<decimal> Decimals { get; set; } = [1m, 2m, 4m];
        public List<double> Doubles { get; set; } = [1, 2, 4];
        public List<float> Floats { get; set; } = [1, 2, 4];
        public List<int?> NullableInts { get; set; } = [1, 2, null, 4];
        public List<decimal?> NullableDecimals { get; set; } = [1m, 2m, null, 4m];
        public List<DateTime> Dates { get; set; } = [new(2024, 5, 1), new(2024, 1, 1), new(2024, 9, 1)];
        public List<string> Names { get; set; } = ["b", "a", "c"];
        public List<TimeSpan> Durations { get; set; } = [TimeSpan.FromHours(1)];
        public List<TimeSpan?> NullableDurations { get; set; } = [TimeSpan.FromHours(1), null];
    }

    // Aggregations take their source from the chained target, so the whole selector chain has
    // to be walked (a single PropertyAccess call only covers one part).
    private static TResult Evaluate<TResult>(PropertySelector<TestOrder> selector, TestOrder item)
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestOrder, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private static TResult Evaluate<TResult>(PropertySelector selector, TestOrder item)
    {
        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var lambda = Expression.Lambda<Func<TestOrder, TResult>>(body, parameter).Compile();
        return lambda(item);
    }

    private const string Delimiter = SelectorDefaults.PropertyDelimiter;

    private static TestOrder SampleOrder() => new()
    {
        Lines =
        [
            new TestLine { Amount = 30, Weight = 1.5 },
            new TestLine { Amount = 10, Weight = 2.5 },
            new TestLine { Amount = 20, Weight = 3.0 },
        ],
        Numbers = [10, 20, 30],
        Weights = [1.5, 2.5],
        Code = "abc",
    };

    // ── Count ───────────────────────────────────────────────────────────────

    [Fact]
    public void Count_OnCollectionProperty_ReturnsElementCount()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(3, result);
    }

    [Fact]
    public void Count_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, new TestOrder());

        Assert.Equal(0, result);
    }

    [Fact]
    public void Count_OnComplexElementCollection_ReturnsElementCount()
    {
        PropertySelector<TestOrder> selector = $"Lines{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(3, result);
    }

    [Fact]
    public void Count_OnNonGenericSelector_ReturnsElementCount()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(3, result);
    }

    [Fact]
    public void Count_OnNonEnumerableProperty_ThrowsSelectorParseExceptionAtParseTime()
    {
        // The generic selector validates enumerability in its constructor.
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Code{Delimiter}{SelectorDefaults.CountAggregateFunction}";
        });
    }

    [Fact]
    public void Count_WithParameters_ThrowsSelectorParseException()
    {
        // Aggregation selectors only declare a parameterless constructor.
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountAggregateFunction}(Amount)";
        });
    }

    // ── LongCount ───────────────────────────────────────────────────────────

    [Fact]
    public void LongCount_OnCollectionProperty_ReturnsElementCountAsLong()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        var result = Evaluate<long>(selector, SampleOrder());

        Assert.Equal(typeof(long), selector.Type);
        Assert.Equal(3L, result);
    }

    [Fact]
    public void LongCount_EmitsALongCountThatMatchesTheDeclaredSelectorType()
    {
        // Regression guard: the implementation used to bind Enumerable.Count, so the compiled
        // expression yielded an int while SelectorType declared long.
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);

        Assert.Equal(typeof(long), body.Type);
        Assert.Equal(selector.Type, body.Type);
    }

    [Fact]
    public void LongCount_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        Assert.Equal(0L, Evaluate<long>(selector, new TestOrder()));
    }

    [Fact]
    public void LongCount_OnNonGenericSelector_ReturnsElementCountAsLong()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.LongCountAggregateFunction}";

        Assert.Equal(3L, Evaluate<long>(selector, SampleOrder()));
    }

    // ── CountDistinct ───────────────────────────────────────────────────────

    [Fact]
    public void CountDistinct_CountsEachValueOnce()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";

        var result = Evaluate<int>(selector, new TestOrder { Numbers = [10, 20, 10, 30, 20] });

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountDistinct_OnProjectedCollection_CountsDistinctValues()
    {
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";
        var order = new TestOrder { Lines = [new() { Amount = 5 }, new() { Amount = 5 }, new() { Amount = 7 }] };

        Assert.Equal(2, Evaluate<int>(selector, order));
    }

    [Fact]
    public void CountDistinct_SkipsNulls()
    {
        // Like SQL's COUNT(DISTINCT x), so the in-memory and translated results agree.
        PropertySelector<TestOrder> selector = $"NullableInts{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";

        Assert.Equal(3, Evaluate<int>(selector, new TestOrder { NullableInts = [1, null, 2, null, 1, 4] }));
        Assert.Equal(0, Evaluate<int>(selector, new TestOrder { NullableInts = [null, null] }));
    }

    [Fact]
    public void CountDistinct_SkipsNullReferences()
    {
        PropertySelector<TestOrder> selector = $"Names{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";

        Assert.Equal(2, Evaluate<int>(selector, new TestOrder { Names = ["a", null!, "b", "a"] }));
    }

    [Fact]
    public void CountDistinct_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";

        Assert.Equal(0, Evaluate<int>(selector, new TestOrder()));
    }

    [Fact]
    public void CountDistinct_OnNonGenericSelector_CountsEachValueOnce()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";

        Assert.Equal(3, Evaluate<int>(selector, new TestOrder { Numbers = [10, 20, 10, 30] }));
    }

    [Fact]
    public void CountDistinct_IsNotMistakenForCount()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";

        Assert.Equal($"Numbers{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}", selector.ToString());
    }

    [Fact]
    public void CountDistinct_OnNonEnumerableProperty_ThrowsSelectorParseExceptionAtParseTime()
    {
        Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Code{Delimiter}{SelectorDefaults.CountDistinctAggregateFunction}";
        });
    }

    // ── Sum ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Sum_OnIntegerCollection_ReturnsTotal()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(typeof(int), selector.Type);
        Assert.Equal(60, result);
    }

    [Fact]
    public void Sum_OnProjectedCollectionProperty_SumsTheProjectedValues()
    {
        // "Lines.Amount" projects the collection with Select before the aggregation runs.
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(60, result);
    }

    [Fact]
    public void Sum_OnEmptyCollection_ReturnsZero()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, new TestOrder());

        Assert.Equal(0, result);
    }

    [Fact]
    public void Sum_OnDoubleCollection_ReturnsTotal()
    {
        // Every aggregate used to bind the IEnumerable<int> overload, so this threw an ArgumentException.
        PropertySelector<TestOrder> selector = $"Weights{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        Assert.Equal(4d, Evaluate<double>(selector, SampleOrder()));
    }

    [Fact]
    public void Sum_OnNonGenericSelector_ReturnsTotal()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(60, result);
    }

    // ── Min / Max ───────────────────────────────────────────────────────────

    [Fact]
    public void Min_OnIntegerCollection_ReturnsSmallestValue()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.MinAggregateFunction}";

        var result = Evaluate<int?>(selector, SampleOrder());

        Assert.Equal(typeof(int?), selector.Type);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Max_OnIntegerCollection_ReturnsLargestValue()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.MaxAggregateFunction}";

        var result = Evaluate<int?>(selector, SampleOrder());

        Assert.Equal(typeof(int?), selector.Type);
        Assert.Equal(30, result);
    }

    [Fact]
    public void Max_OnProjectedCollectionProperty_ReturnsLargestProjectedValue()
    {
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.MaxAggregateFunction}";

        var result = Evaluate<int?>(selector, SampleOrder());

        Assert.Equal(30, result);
    }

    [Fact]
    public void Min_OnNonGenericSelector_ReturnsSmallestValue()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.MinAggregateFunction}";

        var result = Evaluate<int?>(selector, SampleOrder());

        Assert.Equal(10, result);
    }

    // ── Average ─────────────────────────────────────────────────────────────

    [Fact]
    public void Avg_OnIntegerCollection_ReturnsDoubleAverage()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var result = Evaluate<double?>(selector, SampleOrder());

        Assert.Equal(typeof(double?), selector.Type);
        Assert.Equal(20d, result);
    }

    [Fact]
    public void Avg_OnProjectedCollectionProperty_ReturnsDoubleAverage()
    {
        PropertySelector<TestOrder> selector = $"Lines.Amount{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var result = Evaluate<double?>(selector, SampleOrder());

        Assert.Equal(20d, result);
    }

    [Fact]
    public void Avg_OnNonGenericSelector_ReturnsDoubleAverage()
    {
        PropertySelector selector = $"Numbers{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var result = Evaluate<double?>(selector, SampleOrder());

        Assert.Equal(20d, result);
    }

    // ── Every numeric element type ──────────────────────────────────────────

    public static TheoryData<string, string, Type, object> ElementTypeCases() => new()
    {
        // Average follows Enumerable.Average over the nullable element: double? for integers and double,
        // float? for float, decimal? for decimal.
        { "Longs", SelectorDefaults.AvgAggregateFunction, typeof(double?), 7d / 3 },
        { "Decimals", SelectorDefaults.AvgAggregateFunction, typeof(decimal?), 7m / 3 },
        { "Doubles", SelectorDefaults.AvgAggregateFunction, typeof(double?), 7d / 3 },
        { "Floats", SelectorDefaults.AvgAggregateFunction, typeof(float?), 7f / 3 },
        { "NullableInts", SelectorDefaults.AvgAggregateFunction, typeof(double?), 7d / 3 },
        { "NullableDecimals", SelectorDefaults.AvgAggregateFunction, typeof(decimal?), 7m / 3 },
        // Sum keeps the element type (an empty sum is 0).
        { "Longs", SelectorDefaults.SumAggregateFunction, typeof(long), 7L },
        { "Decimals", SelectorDefaults.SumAggregateFunction, typeof(decimal), 7m },
        { "Floats", SelectorDefaults.SumAggregateFunction, typeof(float), 7f },
        { "NullableInts", SelectorDefaults.SumAggregateFunction, typeof(int?), 7 },
        // Min and Max return the nullable element type, so an empty collection gives null.
        { "Decimals", SelectorDefaults.MinAggregateFunction, typeof(decimal?), 1m },
        { "NullableDecimals", SelectorDefaults.MinAggregateFunction, typeof(decimal?), 1m },
        { "Longs", SelectorDefaults.MaxAggregateFunction, typeof(long?), 4L },
        { "Doubles", SelectorDefaults.MaxAggregateFunction, typeof(double?), 4d },
        // Min and Max also work on any comparable type, through the generic overload.
        { "Dates", SelectorDefaults.MinAggregateFunction, typeof(DateTime?), new DateTime(2024, 1, 1) },
        { "Names", SelectorDefaults.MaxAggregateFunction, typeof(string), "c" },
    };

    // ── Empty collections ───────────────────────────────────────────────────

    public static TheoryData<string, string, Type> EmptyCollectionCases() => new()
    {
        { "Numbers", SelectorDefaults.MinAggregateFunction, typeof(int?) },
        { "Numbers", SelectorDefaults.MaxAggregateFunction, typeof(int?) },
        { "Numbers", SelectorDefaults.AvgAggregateFunction, typeof(double?) },
        { "Lines.Amount", SelectorDefaults.MaxAggregateFunction, typeof(int?) },
        { "Lines.Weight", SelectorDefaults.AvgAggregateFunction, typeof(double?) },
        { "Decimals", SelectorDefaults.AvgAggregateFunction, typeof(decimal?) },
        { "Floats", SelectorDefaults.MinAggregateFunction, typeof(float?) },
        { "Dates", SelectorDefaults.MaxAggregateFunction, typeof(DateTime?) },
        { "NullableInts", SelectorDefaults.MaxAggregateFunction, typeof(int?) },
        { "Names", SelectorDefaults.MinAggregateFunction, typeof(string) },
    };

    private static TestOrder EmptyOrder() => new() { Longs = [], Decimals = [], Doubles = [], Floats = [], NullableInts = [], NullableDecimals = [], Dates = [], Names = [] };

    // Enumerable.Min/Max/Average throw on an empty sequence of a non-nullable type, and SQL returns NULL.
    [Theory]
    [MemberData(nameof(EmptyCollectionCases))]
    public void MinMaxAvg_OnEmptyCollection_ReturnNull(string property, string function, Type expectedType)
    {
        PropertySelector<TestOrder> selector = $"{property}{Delimiter}{function}";

        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var value = Expression.Lambda<Func<TestOrder, object?>>(Expression.Convert(body, typeof(object)), parameter).Compile()(EmptyOrder());

        Assert.Equal(expectedType, selector.Type);
        Assert.Equal(expectedType, body.Type);
        Assert.Null(value);
    }

    [Theory]
    [MemberData(nameof(EmptyCollectionCases))]
    public void MinMaxAvg_NonGeneric_OnEmptyCollection_ReturnNull(string property, string function, Type expectedType)
    {
        PropertySelector selector = $"{property}{Delimiter}{function}";

        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var value = Expression.Lambda<Func<TestOrder, object?>>(Expression.Convert(body, typeof(object)), parameter).Compile()(EmptyOrder());

        Assert.Equal(expectedType, body.Type);
        Assert.Null(value);
    }

    [Fact]
    public void Max_InArithmetic_OnEmptyCollection_IsNull()
    {
        PropertySelector<TestOrder> selector = $"Numbers{Delimiter}{SelectorDefaults.MaxAggregateFunction} + 1";

        Assert.Null(Evaluate<int?>(selector, new TestOrder()));
        Assert.Equal(31, Evaluate<int?>(selector, SampleOrder()));
    }

    [Theory]
    [MemberData(nameof(ElementTypeCases))]
    public void Aggregate_FollowsTheElementType(string property, string function, Type expectedType, object expectedValue)
    {
        PropertySelector<TestOrder> selector = $"{property}{Delimiter}{function}";

        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var value = Expression.Lambda<Func<TestOrder, object>>(Expression.Convert(body, typeof(object)), parameter).Compile()(new TestOrder());

        Assert.Equal(expectedType, selector.Type);
        Assert.Equal(expectedType, body.Type);
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [MemberData(nameof(ElementTypeCases))]
    public void Aggregate_NonGeneric_FollowsTheElementType(string property, string function, Type expectedType, object expectedValue)
    {
        PropertySelector selector = $"{property}{Delimiter}{function}";

        var parameter = Expression.Parameter(typeof(TestOrder), "x");
        var body = ExpressionUtils.CreateMemberAccess(parameter, selector);
        var value = Expression.Lambda<Func<TestOrder, object>>(Expression.Convert(body, typeof(object)), parameter).Compile()(new TestOrder());

        Assert.Equal(expectedType, body.Type);
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void Avg_OnNonNumericElements_ThrowsAtParseTime()
    {
        var exception = Assert.Throws<SelectorParseException>(() =>
        {
            PropertySelector<TestOrder> selector = $"Names{Delimiter}{SelectorDefaults.AvgAggregateFunction}";
        });
        Assert.Contains("Average is not supported for elements of type String", exception.Message);
    }

    [Theory]
    [InlineData("Durations", "TimeSpan")]
    [InlineData("NullableDurations", "TimeSpan?")]
    [InlineData("Dates", "DateTime")]
    public void Avg_OnAnUnsupportedValueType_NamesTheElementType(string collection, string elementType)
    {
        var text = $"{collection}{Delimiter}{SelectorDefaults.AvgAggregateFunction}";

        var generic = Assert.Throws<SelectorParseException>(() => { PropertySelector<TestOrder> selector = text; });
        var nonGeneric = Assert.ThrowsAny<Exception>(() => Evaluate<object>((PropertySelector)text, new TestOrder()));

        Assert.Equal($"Average is not supported for elements of type {elementType}.", generic.Detail);
        Assert.Equal($"Average is not supported for elements of type {elementType}.", nonGeneric.Message);
    }

    [Theory]
    [InlineData("Durations")]
    [InlineData("NullableDurations")]
    public void MinAndMax_OnTimeSpans_StillWork(string collection)
    {
        PropertySelector<TestOrder> min = $"{collection}{Delimiter}{SelectorDefaults.MinAggregateFunction}";
        PropertySelector<TestOrder> max = $"{collection}{Delimiter}{SelectorDefaults.MaxAggregateFunction}";

        Assert.Equal(TimeSpan.FromHours(1), Evaluate<TimeSpan?>(min, new TestOrder()));
        Assert.Equal(TimeSpan.FromHours(1), Evaluate<TimeSpan?>(max, new TestOrder()));
    }

    [Fact]
    public void Sum_OnNonNumericElements_ThrowsOnBuild()
    {
        // Sum declares the element type without checking it, so a string list fails when the call is built.
        PropertySelector<TestOrder> selector = $"Names{Delimiter}{SelectorDefaults.SumAggregateFunction}";

        var exception = Assert.Throws<InvalidOperationException>(() => Evaluate<object>(selector, new TestOrder()));
        Assert.Contains("Sum is not supported for elements of type String", exception.Message);
    }

    // ── Combined with the other families ────────────────────────────────────

    [Fact]
    public void Count_AfterOrdering_CountsTheOrderedSequence()
    {
        PropertySelector<TestOrder> selector =
            $"Numbers{Delimiter}{SelectorDefaults.OrderAscending}{Delimiter}{SelectorDefaults.CountAggregateFunction}";

        var result = Evaluate<int>(selector, SampleOrder());

        Assert.Equal(3, result);
    }
}
