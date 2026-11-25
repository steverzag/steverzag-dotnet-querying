using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddDays, Piped = PipedForm.FirstArgument)]
internal record DateAddDaysSelector<T> : FunctionPropertySelector<T>
{
	public DateAddDaysSelector() : base(SelectorDefaults.DateAddDays) { }
	public DateAddDaysSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddDays, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddDaysSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddDays, Piped = PipedForm.FirstArgument)]
internal record DateAddDaysSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddDays", p[0], p[1])),
	];

	public DateAddDaysSelector() : base(SelectorDefaults.DateAddDays) { }
	public DateAddDaysSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddDays, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
