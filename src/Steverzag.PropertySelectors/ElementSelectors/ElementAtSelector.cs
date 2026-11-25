using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors;

[SelectorToken(SelectorDefaults.ElementAtIndex)]
public record ElementAtSelector<T> : FunctionPropertySelector<T>
{
	public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex) { }
	public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => ElementAtSelector.Table;
}

[SelectorToken(SelectorDefaults.ElementAtIndex)]
public record ElementAtSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.IntegerLiteral], (t, _) => ResultTypes.ElementOf(t), p => target => target!.CreateElementAtFunction(p[0]))
		{ Target = ArgumentConstraint.Collection },
	];

	public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex) { }
	public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
