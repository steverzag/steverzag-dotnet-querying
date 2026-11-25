using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringSubstring)]
internal record StringSubstringSelector<T> : FunctionPropertySelector<T>
{
	public StringSubstringSelector() : base(SelectorDefaults.StringSubstring) { }
	public StringSubstringSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringSubstring, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringSubstringSelector.Table;
}

[SelectorToken(SelectorDefaults.StringSubstring)]
internal record StringSubstringSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Integer], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringSubstringMethodInfo, p[0]))
		{ Target = ArgumentConstraint.String },
		new([ArgumentConstraint.Integer, ArgumentConstraint.Integer], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringSubstring_2_MethodInfo, p[0], p[1]))
		{ Target = ArgumentConstraint.String },
	];

	public StringSubstringSelector() : base(SelectorDefaults.StringSubstring) { }
	public StringSubstringSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringSubstring, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
