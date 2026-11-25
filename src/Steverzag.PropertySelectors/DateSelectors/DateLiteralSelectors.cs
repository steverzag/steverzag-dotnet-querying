using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.ValueSelectors;
using System.Globalization;
using System.Linq.Expressions;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateTimeLiteral, Piped = PipedForm.Rejected)]
internal record DateTimeLiteralSelector<T> : FunctionPropertySelector<T>
{
	public DateTimeLiteralSelector() : base(SelectorDefaults.DateTimeLiteral) { }
	public DateTimeLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateTimeLiteral, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateTimeLiteralSelector.Table;
}

[SelectorToken(SelectorDefaults.DateTimeLiteral, Piped = PipedForm.Rejected)]
internal record DateTimeLiteralSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table = [DateLiterals.Signature(SelectorDefaults.DateTimeLiteral, DateLiterals.ParseDateTime)];

	public DateTimeLiteralSelector() : base(SelectorDefaults.DateTimeLiteral) { }
	public DateTimeLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateTimeLiteral, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}

[SelectorToken(SelectorDefaults.DateOnlyLiteral, Piped = PipedForm.Rejected)]
internal record DateOnlyLiteralSelector<T> : FunctionPropertySelector<T>
{
	public DateOnlyLiteralSelector() : base(SelectorDefaults.DateOnlyLiteral) { }
	public DateOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateOnlyLiteral, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateOnlyLiteralSelector.Table;
}

[SelectorToken(SelectorDefaults.DateOnlyLiteral, Piped = PipedForm.Rejected)]
internal record DateOnlyLiteralSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table = [DateLiterals.Signature(SelectorDefaults.DateOnlyLiteral, DateLiterals.ParseDateOnly)];

	public DateOnlyLiteralSelector() : base(SelectorDefaults.DateOnlyLiteral) { }
	public DateOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateOnlyLiteral, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}

[SelectorToken(SelectorDefaults.TimeOnlyLiteral, Piped = PipedForm.Rejected)]
internal record TimeOnlyLiteralSelector<T> : FunctionPropertySelector<T>
{
	public TimeOnlyLiteralSelector() : base(SelectorDefaults.TimeOnlyLiteral) { }
	public TimeOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.TimeOnlyLiteral, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => TimeOnlyLiteralSelector.Table;
}

[SelectorToken(SelectorDefaults.TimeOnlyLiteral, Piped = PipedForm.Rejected)]
internal record TimeOnlyLiteralSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table = [DateLiterals.Signature(SelectorDefaults.TimeOnlyLiteral, DateLiterals.ParseTimeOnly)];

	public TimeOnlyLiteralSelector() : base(SelectorDefaults.TimeOnlyLiteral) { }
	public TimeOnlyLiteralSelector(List<PropertySelector> parameters) : base(SelectorDefaults.TimeOnlyLiteral, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}

internal static class DateLiterals
{
	internal static DateTime ParseDateTime(string text) => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	internal static DateOnly ParseDateOnly(string text) => DateOnly.Parse(text, CultureInfo.InvariantCulture);
	internal static TimeOnly ParseTimeOnly(string text) => TimeOnly.Parse(text, CultureInfo.InvariantCulture);

	internal static FunctionSignature Signature<TValue>(string token, Func<string, TValue> parse) =>
		new([ArgumentConstraint.StringLiteral], ResultTypes.Fixed(typeof(TValue)), parameters =>
		{
			var literal = (IStringLiteralSelector)parameters[0];

			TValue value;
			try
			{
				value = parse(literal.Value);
			}
			catch (FormatException e)
			{
				throw new ArgumentException($"Invalid {token} literal \"{literal.Value}\": {e.Message}", e);
			}

			return _ => Expression.Constant(value, typeof(TValue));
		});
}
