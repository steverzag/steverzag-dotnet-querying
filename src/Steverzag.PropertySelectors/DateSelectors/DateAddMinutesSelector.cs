using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMinutes, Piped = PipedForm.FirstArgument)]
internal record DateAddMinutesSelector<T> : FunctionPropertySelector<T>
{
	public DateAddMinutesSelector() : base(SelectorDefaults.DateAddMinutes) { }
	public DateAddMinutesSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMinutes, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddMinutesSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddMinutes, Piped = PipedForm.FirstArgument)]
internal record DateAddMinutesSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddMinutes", p[0], p[1])),
	];

	public DateAddMinutesSelector() : base(SelectorDefaults.DateAddMinutes) { }
	public DateAddMinutesSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMinutes, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
