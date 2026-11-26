using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringStartsWith, Category = FunctionCategory.String)]
internal record StringStartsWithSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringStartsWithSelector() : base(SelectorDefaults.StringStartsWith) { }
	public StringStartsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringStartsWith, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringStartsWithSelector.Table;
}

[SelectorToken(SelectorDefaults.StringStartsWith, Category = FunctionCategory.String)]
internal record StringStartsWithSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringStartsWithMethodInfo, p[0]))
		{ Target = ArgumentConstraint.String },
	];

	public StringStartsWithSelector() : base(SelectorDefaults.StringStartsWith) { }
	public StringStartsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringStartsWith, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
