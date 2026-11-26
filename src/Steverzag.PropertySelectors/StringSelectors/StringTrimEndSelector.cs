using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrimEnd, Category = FunctionCategory.String)]
internal record StringTrimEndSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringTrimEndSelector() : base(SelectorDefaults.StringTrimEnd) { }
	public StringTrimEndSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimEnd, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringTrimEndSelector.Table;
}

[SelectorToken(SelectorDefaults.StringTrimEnd, Category = FunctionCategory.String)]
internal record StringTrimEndSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringTrimEndMethodInfo))
		{ Target = ArgumentConstraint.String },
	];

	public StringTrimEndSelector() : base(SelectorDefaults.StringTrimEnd) { }
	public StringTrimEndSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimEnd, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
