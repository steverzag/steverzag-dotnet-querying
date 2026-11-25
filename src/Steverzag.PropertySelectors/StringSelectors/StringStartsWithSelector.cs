using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringStartsWith)]
internal record StringStartsWithSelector<T> : FunctionPropertySelector<T>
{
	public StringStartsWithSelector() : base(SelectorDefaults.StringStartsWith) { }
	public StringStartsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringStartsWith, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringStartsWithSelector.Table;
}

[SelectorToken(SelectorDefaults.StringStartsWith)]
internal record StringStartsWithSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringStartsWithMethodInfo, p[0]))
		{ Target = ArgumentConstraint.String },
	];

	public StringStartsWithSelector() : base(SelectorDefaults.StringStartsWith) { }
	public StringStartsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringStartsWith, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
