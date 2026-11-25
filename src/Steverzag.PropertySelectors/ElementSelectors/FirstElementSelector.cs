using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors;

[SelectorToken(SelectorDefaults.ElementFirst)]
public record FirstElementSelector<T> : ElementFunctionSelector<T>
{
	public FirstElementSelector() : base(SelectorDefaults.ElementFirst) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => FirstElementSelector.Table;
}

[SelectorToken(SelectorDefaults.ElementFirst)]
public record FirstElementSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => ResultTypes.ElementOf(t), _ => target => target!.CreateElementFirstOrDefaultFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public FirstElementSelector() : base(SelectorDefaults.ElementFirst) { }
	public FirstElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementFirst, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
