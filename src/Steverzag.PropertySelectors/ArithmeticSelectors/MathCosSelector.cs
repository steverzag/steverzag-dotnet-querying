using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathCos, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathCosSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathCosSelector.Table;
}

[SelectorToken(SelectorDefaults.MathCos, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathCosSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathCosMethodInfo, p[0])),
	];

	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
