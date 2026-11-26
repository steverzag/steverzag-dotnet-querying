using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringCompare, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringCompareSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringCompareSelector() : base(SelectorDefaults.StringCompare) { }
	public StringCompareSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringCompare, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringCompareSelector.Table;
}

[SelectorToken(SelectorDefaults.StringCompare, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringCompareSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(int)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringCompareMethodInfo, p[0], p[1])),
	];

	public StringCompareSelector() : base(SelectorDefaults.StringCompare) { }
	public StringCompareSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringCompare, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
