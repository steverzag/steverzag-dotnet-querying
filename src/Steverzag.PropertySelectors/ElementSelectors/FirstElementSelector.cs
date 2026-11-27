using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors;

[SelectorToken(SelectorDefaults.ElementFirst, Category = FunctionCategory.Element)]
internal record FirstElementSelector<T> : ElementFunctionSelector<T>, IFunctionSignatures
{
	public FirstElementSelector() : base(SelectorDefaults.ElementFirst) { }
	public static IReadOnlyList<FunctionSignature> Signatures => FirstElementSelector.Table;
}

[SelectorToken(SelectorDefaults.ElementFirst, Category = FunctionCategory.Element)]
internal record FirstElementSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => ResultTypes.ElementOf(t), _ => target => target!.CreateElementFirstOrDefaultFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public FirstElementSelector() : base(SelectorDefaults.ElementFirst) { }
	public FirstElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementFirst, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
