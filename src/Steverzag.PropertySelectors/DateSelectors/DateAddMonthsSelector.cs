using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMonths, Piped = PipedForm.FirstArgument)]
internal record DateAddMonthsSelector<T> : FunctionPropertySelector<T>
{
	public DateAddMonthsSelector() : base(SelectorDefaults.DateAddMonths) { }
	public DateAddMonthsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMonths, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddMonthsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddMonths, Piped = PipedForm.FirstArgument)]
internal record DateAddMonthsSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddMonths", p[0], p[1])),
	];

	public DateAddMonthsSelector() : base(SelectorDefaults.DateAddMonths) { }
	public DateAddMonthsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMonths, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
