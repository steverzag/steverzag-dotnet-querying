using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddHours, Piped = PipedForm.FirstArgument)]
internal record DateAddHoursSelector<T> : FunctionPropertySelector<T>
{
	public DateAddHoursSelector() : base(SelectorDefaults.DateAddHours) { }
	public DateAddHoursSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddHours, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddHoursSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddHours, Piped = PipedForm.FirstArgument)]
internal record DateAddHoursSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddHours", p[0], p[1])),
	];

	public DateAddHoursSelector() : base(SelectorDefaults.DateAddHours) { }
	public DateAddHoursSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddHours, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
