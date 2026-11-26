using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringToLower, Category = FunctionCategory.String)]
internal record StringToLowerSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringToLowerSelector() : base(SelectorDefaults.StringToLower) { }
	public StringToLowerSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToLower, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringToLowerSelector.Table;
}

[SelectorToken(SelectorDefaults.StringToLower, Category = FunctionCategory.String)]
internal record StringToLowerSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringToLowerMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringToLowerSelector() : base(SelectorDefaults.StringToLower) { }
	public StringToLowerSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToLower, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
