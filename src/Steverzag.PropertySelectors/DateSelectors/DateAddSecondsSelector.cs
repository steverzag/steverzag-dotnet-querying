using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddSeconds, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddSecondsSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddSecondsSelector() : base(SelectorDefaults.DateAddSeconds) { }
	public DateAddSecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddSeconds, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddSecondsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddSeconds, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddSecondsSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddSeconds"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddSeconds", p[0], p[1])),
	];

	public DateAddSecondsSelector() : base(SelectorDefaults.DateAddSeconds) { }
	public DateAddSecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddSeconds, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
