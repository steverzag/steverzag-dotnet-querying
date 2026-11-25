using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringToLower)]
internal record StringToLowerSelector<T> : FunctionPropertySelector<T>
{
	public StringToLowerSelector() : base(SelectorDefaults.StringToLower) { }
	public StringToLowerSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToLower, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringToLowerSelector.Table;
}

[SelectorToken(SelectorDefaults.StringToLower)]
internal record StringToLowerSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringToLowerMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringToLowerSelector() : base(SelectorDefaults.StringToLower) { }
	public StringToLowerSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToLower, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
