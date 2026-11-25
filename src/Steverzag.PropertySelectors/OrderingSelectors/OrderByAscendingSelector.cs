using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.OrderingSelectors;

[SelectorToken(SelectorDefaults.OrderAscending, Arguments = ArgumentScope.TargetElement)]
public record OrderByAscendingSelector<T> : OrderingFunctionSelector<T>
{
	public OrderByAscendingSelector() : base(SelectorDefaults.OrderAscending) { }
	public OrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderAscending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => OrderByAscendingSelector.Table;
}

[SelectorToken(SelectorDefaults.OrderAscending, Arguments = ArgumentScope.TargetElement)]
public record OrderByAscendingSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.OrderedType(t), _ => target => target!.CreateOrderByAscendingFunction())
		{ Target = ArgumentConstraint.Collection },
		new([ArgumentConstraint.Any], (t, _) => Implementations.OrderedType(t), p => target => target!.CreateOrderByAscendingFunction(p[0]))
		{ Target = ArgumentConstraint.Collection },
	];

	public OrderByAscendingSelector() : base(SelectorDefaults.OrderAscending) { }
	public OrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderAscending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
