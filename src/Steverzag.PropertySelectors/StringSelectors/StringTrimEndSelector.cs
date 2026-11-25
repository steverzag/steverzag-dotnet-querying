using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrimEnd)]
internal record StringTrimEndSelector<T> : FunctionPropertySelector<T>
{
	public StringTrimEndSelector() : base(SelectorDefaults.StringTrimEnd) { }
	public StringTrimEndSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimEnd, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringTrimEndSelector.Table;
}

[SelectorToken(SelectorDefaults.StringTrimEnd)]
internal record StringTrimEndSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringTrimEndMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringTrimEndSelector() : base(SelectorDefaults.StringTrimEnd) { }
	public StringTrimEndSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimEnd, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
