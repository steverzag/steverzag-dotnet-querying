using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIndexOf, Category = FunctionCategory.String)]
internal record StringIndexOfSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringIndexOfSelector() : base(SelectorDefaults.StringIndexOf) { }
	public StringIndexOfSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIndexOf, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringIndexOfSelector.Table;
}

[SelectorToken(SelectorDefaults.StringIndexOf, Category = FunctionCategory.String)]
internal record StringIndexOfSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(int)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringIndexOfMethodInfo, p[0]))
		{ Target = ArgumentConstraint.String },
	];

	public StringIndexOfSelector() : base(SelectorDefaults.StringIndexOf) { }
	public StringIndexOfSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIndexOf, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
