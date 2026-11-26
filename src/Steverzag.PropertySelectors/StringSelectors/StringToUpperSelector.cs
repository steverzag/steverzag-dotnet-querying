using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringToUpper, Category = FunctionCategory.String)]
internal record StringToUpperSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringToUpperSelector() : base(SelectorDefaults.StringToUpper) { }
	public StringToUpperSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToUpper, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringToUpperSelector.Table;
}

[SelectorToken(SelectorDefaults.StringToUpper, Category = FunctionCategory.String)]
internal record StringToUpperSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringToUpperMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringToUpperSelector() : base(SelectorDefaults.StringToUpper) { }
	public StringToUpperSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToUpper, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
