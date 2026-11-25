using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMilliseconds, Piped = PipedForm.FirstArgument)]
internal record DateAddMillisecondsSelector<T> : FunctionPropertySelector<T>
{
	public DateAddMillisecondsSelector() : base(SelectorDefaults.DateAddMilliseconds) { }
	public DateAddMillisecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMilliseconds, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddMillisecondsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddMilliseconds, Piped = PipedForm.FirstArgument)]
internal record DateAddMillisecondsSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddMilliseconds", p[0], p[1])),
	];

	public DateAddMillisecondsSelector() : base(SelectorDefaults.DateAddMilliseconds) { }
	public DateAddMillisecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMilliseconds, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
