using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringContains)]
internal record StringContainsSelector<T> : FunctionPropertySelector<T>
{
	public StringContainsSelector() : base(SelectorDefaults.StringContains) { }
	public StringContainsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringContains, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringContainsSelector.Table;
}

[SelectorToken(SelectorDefaults.StringContains)]
internal record StringContainsSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringContainsMethodInfo, p[0]))
		{ Target = ArgumentConstraint.String },
	];

	public StringContainsSelector() : base(SelectorDefaults.StringContains) { }
	public StringContainsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringContains, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
