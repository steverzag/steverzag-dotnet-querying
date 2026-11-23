using Steverzag.PropertySelectors;

namespace Steverzag.PropertySelectors.Tests;

public class SelectorDefaultsTests
{
    [Fact]
    public void FunctionStart_ShouldHaveCorrectPrefix()
    {
        // Assert
        Assert.Equal("$FN_", SelectorDefaults.FunctionStart);
    }

    [Fact]
    public void SpecifierStart_ShouldHaveCorrectPrefix()
    {
        // Assert
        Assert.Equal("$SPEC_", SelectorDefaults.SpecifierStart);
    }

    [Theory]
    [InlineData(nameof(SelectorDefaults.CountAggregateFunction))]
    [InlineData(nameof(SelectorDefaults.LongCountAggregateFunction))]
    [InlineData(nameof(SelectorDefaults.CountDistinctAggregateFunction))]
    [InlineData(nameof(SelectorDefaults.SumAggregateFunction))]
    [InlineData(nameof(SelectorDefaults.AvgAggregateFunction))]
    [InlineData(nameof(SelectorDefaults.MinAggregateFunction))]
    [InlineData(nameof(SelectorDefaults.MaxAggregateFunction))]
    public void AggregateFunctions_ShouldStartWithFunctionPrefix(string propertyName)
    {
        // Arrange
        var property = typeof(SelectorDefaults).GetField(propertyName);
        var value = property?.GetValue(null) as string;

        // Assert
        Assert.NotNull(value);
        Assert.StartsWith(SelectorDefaults.FunctionStart, value);
        Assert.Contains("AGGR_", value);
    }

    [Theory]
    [InlineData(nameof(SelectorDefaults.ElementFirst))]
    [InlineData(nameof(SelectorDefaults.ElementLast))]
    [InlineData(nameof(SelectorDefaults.ElementAtIndex))]
    public void ElementSelectors_ShouldStartWithFunctionPrefix(string propertyName)
    {
        // Arrange
        var property = typeof(SelectorDefaults).GetField(propertyName);
        var value = property?.GetValue(null) as string;

        // Assert
        Assert.NotNull(value);
        Assert.StartsWith(SelectorDefaults.FunctionStart, value);
        Assert.Contains("ELM_", value);
    }

    [Theory]
    [InlineData(nameof(SelectorDefaults.OrderAscending))]
    [InlineData(nameof(SelectorDefaults.OrderDescending))]
    [InlineData(nameof(SelectorDefaults.ThenOrderByAscending))]
    [InlineData(nameof(SelectorDefaults.ThenOrderByDescending))]
    public void OrderingSelectors_ShouldStartWithFunctionPrefix(string propertyName)
    {
        // Arrange
        var property = typeof(SelectorDefaults).GetField(propertyName);
        var value = property?.GetValue(null) as string;

        // Assert
        Assert.NotNull(value);
        Assert.StartsWith(SelectorDefaults.FunctionStart, value);
        Assert.Contains("ORDER", value);
    }

    [Theory]
    [InlineData(nameof(SelectorDefaults.IntegerValueSpecifier))]
    [InlineData(nameof(SelectorDefaults.DoubleValueSpecifier))]
    [InlineData(nameof(SelectorDefaults.BooleanValueSpecifier))]
    [InlineData(nameof(SelectorDefaults.StringValueSpecifier))]
    public void ValueSpecifiers_ShouldStartWithValuePrefix(string propertyName)
    {
        // Arrange
        var property = typeof(SelectorDefaults).GetField(propertyName);
        var value = property?.GetValue(null) as string;

        // Assert
        Assert.NotNull(value);
        Assert.StartsWith(SelectorDefaults.ValueStart, value);
    }

    [Fact]
    public void AllConstants_ShouldBeUnique()
    {
        // Arrange
        var fields = typeof(SelectorDefaults)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != SelectorDefaults.FunctionStart && v != SelectorDefaults.SpecifierStart)
            .ToList();

        // Assert
        Assert.Equal(fields.Count, fields.Distinct().Count());
    }
}