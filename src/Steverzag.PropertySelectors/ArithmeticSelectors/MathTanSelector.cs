using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathTan, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathTanSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathTanSelector() : base(SelectorDefaults.MathTan) { }
	public MathTanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTan, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathTanSelector.Table;
}

[SelectorToken(SelectorDefaults.MathTan, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathTanSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathTanMethodInfo, p[0])),
	];

	public MathTanSelector() : base(SelectorDefaults.MathTan) { }
	public MathTanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTan, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
