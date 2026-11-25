using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors;

[SelectorToken(SelectorDefaults.ElementLast)]
public record LastElementSelector<T> : ElementFunctionSelector<T>
{
	public LastElementSelector() : base(SelectorDefaults.ElementLast) { }
	public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => LastElementSelector.Table;
}

[SelectorToken(SelectorDefaults.ElementLast)]
public record LastElementSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => ResultTypes.ElementOf(t), _ => target => target!.CreateElementLastOrDefaultFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public LastElementSelector() : base(SelectorDefaults.ElementLast) { }
	public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
