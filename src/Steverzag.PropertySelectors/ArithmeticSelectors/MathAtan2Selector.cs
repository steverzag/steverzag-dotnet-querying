using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAtan2, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathAtan2Selector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathAtan2Selector() : base(SelectorDefaults.MathAtan2) { }
	public MathAtan2Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan2, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathAtan2Selector.Table;
}

[SelectorToken(SelectorDefaults.MathAtan2, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathAtan2Selector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateBinaryMathFunction(MethodInfos.MathAtan2MethodInfo, p[0], p[1])),
	];

	public MathAtan2Selector() : base(SelectorDefaults.MathAtan2) { }
	public MathAtan2Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan2, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
