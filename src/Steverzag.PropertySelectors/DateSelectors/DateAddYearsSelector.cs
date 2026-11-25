using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddYears, Piped = PipedForm.FirstArgument)]
internal record DateAddYearsSelector<T> : FunctionPropertySelector<T>
{
	public DateAddYearsSelector() : base(SelectorDefaults.DateAddYears) { }
	public DateAddYearsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddYears, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateAddYearsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddYears, Piped = PipedForm.FirstArgument)]
internal record DateAddYearsSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.DateLike, ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddYears", p[0], p[1])),
	];

	public DateAddYearsSelector() : base(SelectorDefaults.DateAddYears) { }
	public DateAddYearsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddYears, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
