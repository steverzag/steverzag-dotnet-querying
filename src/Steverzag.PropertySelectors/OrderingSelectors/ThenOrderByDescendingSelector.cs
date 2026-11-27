using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.OrderingSelectors;

[SelectorToken(SelectorDefaults.ThenOrderByDescending, Arguments = ArgumentScope.TargetElement, Category = FunctionCategory.Ordering)]
internal record ThenOrderByDescendingSelector<T> : OrderingFunctionSelector<T>, IFunctionSignatures
{
	public ThenOrderByDescendingSelector() : base(SelectorDefaults.ThenOrderByDescending) { }
	public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByDescending, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => ThenOrderByDescendingSelector.Table;
}

[SelectorToken(SelectorDefaults.ThenOrderByDescending, Arguments = ArgumentScope.TargetElement, Category = FunctionCategory.Ordering)]
internal record ThenOrderByDescendingSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.OrderedType(t), _ => target => target!.CreateThenOrderByDescendingFunction())
		{ Target = Implementations.OrderedCollection },
		new([ArgumentConstraint.Any], (t, _) => Implementations.OrderedType(t), p => target => target!.CreateThenOrderByDescendingFunction(p[0]))
		{ Target = Implementations.OrderedCollection },
	];

	public ThenOrderByDescendingSelector() : base(SelectorDefaults.ThenOrderByDescending) { }
	public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByDescending, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
