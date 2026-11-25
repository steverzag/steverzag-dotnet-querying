using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrimStart)]
internal record StringTrimStartSelector<T> : FunctionPropertySelector<T>
{
	public StringTrimStartSelector() : base(SelectorDefaults.StringTrimStart) { }
	public StringTrimStartSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimStart, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringTrimStartSelector.Table;
}

[SelectorToken(SelectorDefaults.StringTrimStart)]
internal record StringTrimStartSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringTrimStartMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringTrimStartSelector() : base(SelectorDefaults.StringTrimStart) { }
	public StringTrimStartSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimStart, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
