using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMilliseconds, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddMillisecondsSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddMillisecondsSelector() : base(SelectorDefaults.DateAddMilliseconds) { }
	public DateAddMillisecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMilliseconds, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddMillisecondsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddMilliseconds, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddMillisecondsSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddMilliseconds"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddMilliseconds", p[0], p[1])),
	];

	public DateAddMillisecondsSelector() : base(SelectorDefaults.DateAddMilliseconds) { }
	public DateAddMillisecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMilliseconds, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
