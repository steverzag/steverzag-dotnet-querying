using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringReplace)]
internal record StringReplaceSelector<T> : FunctionPropertySelector<T>
{
	public StringReplaceSelector() : base(SelectorDefaults.StringReplace) { }
	public StringReplaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringReplace, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringReplaceSelector.Table;
}

[SelectorToken(SelectorDefaults.StringReplace)]
internal record StringReplaceSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringReplaceMethodInfo, p[0], p[1]))
		{ Target = ArgumentConstraint.String },
	];

	public StringReplaceSelector() : base(SelectorDefaults.StringReplace) { }
	public StringReplaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringReplace, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
