using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSqrt, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathSqrtSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathSqrtSelector() : base(SelectorDefaults.MathSqrt) { }
	public MathSqrtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSqrt, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathSqrtSelector.Table;
}

[SelectorToken(SelectorDefaults.MathSqrt, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathSqrtSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathSqrtMethodInfo, p[0])),
	];

	public MathSqrtSelector() : base(SelectorDefaults.MathSqrt) { }
	public MathSqrtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSqrt, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
