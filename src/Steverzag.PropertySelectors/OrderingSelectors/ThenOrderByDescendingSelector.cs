using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.OrderingSelectors;

[SelectorToken(SelectorDefaults.ThenOrderByDescending, Arguments = ArgumentScope.TargetElement)]
public record ThenOrderByDescendingSelector<T> : OrderingFunctionSelector<T>
{
	public ThenOrderByDescendingSelector() : base(SelectorDefaults.ThenOrderByDescending) { }
	public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByDescending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => ThenOrderByDescendingSelector.Table;
}

[SelectorToken(SelectorDefaults.ThenOrderByDescending, Arguments = ArgumentScope.TargetElement)]
public record ThenOrderByDescendingSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.OrderedType(t), _ => target => target!.CreateThenOrderByDescendingFunction())
		{ Target = ArgumentConstraint.Collection },
		new([ArgumentConstraint.Any], (t, _) => Implementations.OrderedType(t), p => target => target!.CreateThenOrderByDescendingFunction(p[0]))
		{ Target = ArgumentConstraint.Collection },
	];

	public ThenOrderByDescendingSelector() : base(SelectorDefaults.ThenOrderByDescending) { }
	public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByDescending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
