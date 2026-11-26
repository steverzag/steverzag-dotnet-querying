using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrimStart, Category = FunctionCategory.String)]
internal record StringTrimStartSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringTrimStartSelector() : base(SelectorDefaults.StringTrimStart) { }
	public StringTrimStartSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimStart, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringTrimStartSelector.Table;
}

[SelectorToken(SelectorDefaults.StringTrimStart, Category = FunctionCategory.String)]
internal record StringTrimStartSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringTrimStartMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringTrimStartSelector() : base(SelectorDefaults.StringTrimStart) { }
	public StringTrimStartSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimStart, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
