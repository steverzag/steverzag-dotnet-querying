using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrim, Category = FunctionCategory.String)]
internal record StringTrimSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringTrimSelector() : base(SelectorDefaults.StringTrim) { }
	public StringTrimSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrim, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringTrimSelector.Table;
}

[SelectorToken(SelectorDefaults.StringTrim, Category = FunctionCategory.String)]
internal record StringTrimSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringTrimMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringTrimSelector() : base(SelectorDefaults.StringTrim) { }
	public StringTrimSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrim, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
