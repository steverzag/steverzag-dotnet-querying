using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathPower, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathPowSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathPowSelector() : base(SelectorDefaults.MathPower) { }
	public MathPowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathPower, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathPowSelector.Table;
}

[SelectorToken(SelectorDefaults.MathPower, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathPowSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateBinaryMathFunction(MethodInfos.MathPowerMethodInfo, p[0], p[1])),
	];

	public MathPowSelector() : base(SelectorDefaults.MathPower) { }
	public MathPowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathPower, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
