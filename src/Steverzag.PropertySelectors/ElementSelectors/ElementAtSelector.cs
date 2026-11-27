using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors;

[SelectorToken(SelectorDefaults.ElementAtIndex, Category = FunctionCategory.Element)]
internal record ElementAtSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex) { }
	public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => ElementAtSelector.Table;
}

[SelectorToken(SelectorDefaults.ElementAtIndex, Category = FunctionCategory.Element)]
internal record ElementAtSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.IntegerLiteral], (t, _) => ResultTypes.ElementOf(t), p => target => target!.CreateElementAtFunction(p[0]))
		{ Target = ArgumentConstraint.Collection },
	];

	public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex) { }
	public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
