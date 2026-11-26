using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringEndsWith, Category = FunctionCategory.String)]
internal record StringEndsWithSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringEndsWithSelector() : base(SelectorDefaults.StringEndsWith) { }
	public StringEndsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringEndsWith, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringEndsWithSelector.Table;
}

[SelectorToken(SelectorDefaults.StringEndsWith, Category = FunctionCategory.String)]
internal record StringEndsWithSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringEndsWithMethodInfo, p[0]))
		{ Target = ArgumentConstraint.String },
	];

	public StringEndsWithSelector() : base(SelectorDefaults.StringEndsWith) { }
	public StringEndsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringEndsWith, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
