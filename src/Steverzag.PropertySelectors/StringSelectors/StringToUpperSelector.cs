using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringToUpper)]
internal record StringToUpperSelector<T> : FunctionPropertySelector<T>
{
	public StringToUpperSelector() : base(SelectorDefaults.StringToUpper) { }
	public StringToUpperSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToUpper, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringToUpperSelector.Table;
}

[SelectorToken(SelectorDefaults.StringToUpper)]
internal record StringToUpperSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringToUpperMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringToUpperSelector() : base(SelectorDefaults.StringToUpper) { }
	public StringToUpperSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToUpper, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
