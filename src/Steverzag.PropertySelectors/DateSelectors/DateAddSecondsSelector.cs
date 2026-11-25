using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddSeconds, Piped = PipedForm.FirstArgument)]
internal record DateAddSecondsSelector<T> : FunctionPropertySelector<T>
{
	public DateAddSecondsSelector() : base(SelectorDefaults.DateAddSeconds) { }
	public DateAddSecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddSeconds, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddSecondsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddSeconds, Piped = PipedForm.FirstArgument)]
internal record DateAddSecondsSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddSeconds", p[0], p[1])),
	];

	public DateAddSecondsSelector() : base(SelectorDefaults.DateAddSeconds) { }
	public DateAddSecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddSeconds, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
