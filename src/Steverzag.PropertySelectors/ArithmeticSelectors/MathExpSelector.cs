using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathExp, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathExpSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathExpSelector() : base(SelectorDefaults.MathExp) { }
	public MathExpSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathExp, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathExpSelector.Table;
}

[SelectorToken(SelectorDefaults.MathExp, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathExpSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathExpMethodInfo, p[0])),
	];

	public MathExpSelector() : base(SelectorDefaults.MathExp) { }
	public MathExpSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathExp, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
