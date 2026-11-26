using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors;

[SelectorToken(SelectorDefaults.ElementLast, Category = FunctionCategory.Element)]
public record LastElementSelector<T> : ElementFunctionSelector<T>, IFunctionSignatures
{
	public LastElementSelector() : base(SelectorDefaults.ElementLast) { }
	public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => LastElementSelector.Table;
}

[SelectorToken(SelectorDefaults.ElementLast, Category = FunctionCategory.Element)]
public record LastElementSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => ResultTypes.ElementOf(t), _ => target => target!.CreateElementLastOrDefaultFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public LastElementSelector() : base(SelectorDefaults.ElementLast) { }
	public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
