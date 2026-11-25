using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrim)]
internal record StringTrimSelector<T> : FunctionPropertySelector<T>
{
	public StringTrimSelector() : base(SelectorDefaults.StringTrim) { }
	public StringTrimSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrim, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringTrimSelector.Table;
}

[SelectorToken(SelectorDefaults.StringTrim)]
internal record StringTrimSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringTrimMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringTrimSelector() : base(SelectorDefaults.StringTrim) { }
	public StringTrimSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrim, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
