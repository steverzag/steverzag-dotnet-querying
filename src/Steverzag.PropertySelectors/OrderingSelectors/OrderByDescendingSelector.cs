using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.OrderingSelectors;

[SelectorToken(SelectorDefaults.OrderDescending, Arguments = ArgumentScope.TargetElement)]
public record OrderByDescendingSelector<T> : OrderingFunctionSelector<T>
{
	public OrderByDescendingSelector() : base(SelectorDefaults.OrderDescending) { }
	public OrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderDescending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => OrderByDescendingSelector.Table;
}

[SelectorToken(SelectorDefaults.OrderDescending, Arguments = ArgumentScope.TargetElement)]
public record OrderByDescendingSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.OrderedType(t), _ => target => target!.CreateOrderByDescendingFunction())
		{ Target = ArgumentConstraint.Collection },
		new([ArgumentConstraint.Any], (t, _) => Implementations.OrderedType(t), p => target => target!.CreateOrderByDescendingFunction(p[0]))
		{ Target = ArgumentConstraint.Collection },
	];

	public OrderByDescendingSelector() : base(SelectorDefaults.OrderDescending) { }
	public OrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderDescending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
