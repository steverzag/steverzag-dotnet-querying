using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAsin, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathAsinSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathAsinSelector() : base(SelectorDefaults.MathAsin) { }
	public MathAsinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAsin, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathAsinSelector.Table;
}

[SelectorToken(SelectorDefaults.MathAsin, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathAsinSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathAsinMethodInfo, p[0])),
	];

	public MathAsinSelector() : base(SelectorDefaults.MathAsin) { }
	public MathAsinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAsin, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
