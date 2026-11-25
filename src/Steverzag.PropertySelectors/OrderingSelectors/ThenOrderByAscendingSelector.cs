using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.OrderingSelectors;

[SelectorToken(SelectorDefaults.ThenOrderByAscending, Arguments = ArgumentScope.TargetElement)]
public record ThenOrderByAscendingSelector<T> : OrderingFunctionSelector<T>
{
	public ThenOrderByAscendingSelector() : base(SelectorDefaults.ThenOrderByAscending) { }
	public ThenOrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByAscending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => ThenOrderByAscendingSelector.Table;
}

[SelectorToken(SelectorDefaults.ThenOrderByAscending, Arguments = ArgumentScope.TargetElement)]
public record ThenOrderByAscendingSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.OrderedType(t), _ => target => target!.CreateThenOrderByAscendingFunction())
		{ Target = ArgumentConstraint.Collection },
		new([ArgumentConstraint.Any], (t, _) => Implementations.OrderedType(t), p => target => target!.CreateThenOrderByAscendingFunction(p[0]))
		{ Target = ArgumentConstraint.Collection },
	];

	public ThenOrderByAscendingSelector() : base(SelectorDefaults.ThenOrderByAscending) { }
	public ThenOrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByAscending, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
