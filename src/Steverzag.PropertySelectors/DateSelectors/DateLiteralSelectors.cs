using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.ValueSelectors;
using System.Globalization;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateTimeLiteral)]
internal record DateTimeLiteralSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(DateTime);
	public DateTimeLiteralSelector() : base(SelectorDefaults.DateTimeLiteral) { }
	public DateTimeLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateTimeLiteral, parameters) { }
	public override void InitializeOverloads() => DateLiterals.AddOverload(Overloads, Parameters, SelectorDefaults.DateTimeLiteral, DateLiterals.ParseDateTime);
}

[SelectorToken(SelectorDefaults.DateTimeLiteral)]
internal record DateTimeLiteralSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(DateTime);
	public DateTimeLiteralSelector() : base(SelectorDefaults.DateTimeLiteral) { }
	public DateTimeLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateTimeLiteral, parameters) { }
	public override void InitializeOverloads() => DateLiterals.AddOverload(Overloads, Parameters, SelectorDefaults.DateTimeLiteral, DateLiterals.ParseDateTime);
}

[SelectorToken(SelectorDefaults.DateOnlyLiteral)]
internal record DateOnlyLiteralSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(DateOnly);
	public DateOnlyLiteralSelector() : base(SelectorDefaults.DateOnlyLiteral) { }
	public DateOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateOnlyLiteral, parameters) { }
	public override void InitializeOverloads() => DateLiterals.AddOverload(Overloads, Parameters, SelectorDefaults.DateOnlyLiteral, DateLiterals.ParseDateOnly);
}

[SelectorToken(SelectorDefaults.DateOnlyLiteral)]
internal record DateOnlyLiteralSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(DateOnly);
	public DateOnlyLiteralSelector() : base(SelectorDefaults.DateOnlyLiteral) { }
	public DateOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateOnlyLiteral, parameters) { }
	public override void InitializeOverloads() => DateLiterals.AddOverload(Overloads, Parameters, SelectorDefaults.DateOnlyLiteral, DateLiterals.ParseDateOnly);
}

[SelectorToken(SelectorDefaults.TimeOnlyLiteral)]
internal record TimeOnlyLiteralSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(TimeOnly);
	public TimeOnlyLiteralSelector() : base(SelectorDefaults.TimeOnlyLiteral) { }
	public TimeOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.TimeOnlyLiteral, parameters) { }
	public override void InitializeOverloads() => DateLiterals.AddOverload(Overloads, Parameters, SelectorDefaults.TimeOnlyLiteral, DateLiterals.ParseTimeOnly);
}

[SelectorToken(SelectorDefaults.TimeOnlyLiteral)]
internal record TimeOnlyLiteralSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(TimeOnly);
	public TimeOnlyLiteralSelector() : base(SelectorDefaults.TimeOnlyLiteral) { }
	public TimeOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.TimeOnlyLiteral, parameters) { }
	public override void InitializeOverloads() => DateLiterals.AddOverload(Overloads, Parameters, SelectorDefaults.TimeOnlyLiteral, DateLiterals.ParseTimeOnly);
}

internal static class DateLiterals
{
	internal static DateTime ParseDateTime(string text) => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	internal static DateOnly ParseDateOnly(string text) => DateOnly.Parse(text, CultureInfo.InvariantCulture);
	internal static TimeOnly ParseTimeOnly(string text) => TimeOnly.Parse(text, CultureInfo.InvariantCulture);

	internal static void AddOverload<TValue>(List<FunctionPropertySelectorOverload> overloads, IReadOnlyList<PropertySelector> parameters,
		string token, Func<string, TValue> parse)
	{
		if (parameters.Count != 1)
			return;

		if (parameters[0] is not IStringLiteralSelector literal)
			throw new ArgumentException($"{token} takes one quoted string, e.g. {token}(\"2024-01-15\"), not '{parameters[0]}'.");

		TValue value;
		try
		{
			value = parse(literal.Value);
		}
		catch (FormatException e)
		{
			throw new ArgumentException($"Invalid {token} literal \"{literal.Value}\": {e.Message}", e);
		}

		var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
		overloads.Add(new FunctionPropertySelectorOverload(valueParameters, _ => Expression.Constant(value, typeof(TValue))));
	}
}
