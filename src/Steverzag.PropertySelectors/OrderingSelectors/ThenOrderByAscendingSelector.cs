using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.OrderingSelectors;

[SelectorToken(SelectorDefaults.ThenOrderByAscending, Arguments = ArgumentScope.TargetElement, Category = FunctionCategory.Ordering)]
public record ThenOrderByAscendingSelector<T> : OrderingFunctionSelector<T>, IFunctionSignatures
{
	public ThenOrderByAscendingSelector() : base(SelectorDefaults.ThenOrderByAscending) { }
	public ThenOrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByAscending, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => ThenOrderByAscendingSelector.Table;
}

[SelectorToken(SelectorDefaults.ThenOrderByAscending, Arguments = ArgumentScope.TargetElement, Category = FunctionCategory.Ordering)]
public record ThenOrderByAscendingSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.OrderedType(t), _ => target => target!.CreateThenOrderByAscendingFunction())
		{ Target = Implementations.OrderedCollection },
		new([ArgumentConstraint.Any], (t, _) => Implementations.OrderedType(t), p => target => target!.CreateThenOrderByAscendingFunction(p[0]))
		{ Target = Implementations.OrderedCollection },
	];

	public ThenOrderByAscendingSelector() : base(SelectorDefaults.ThenOrderByAscending) { }
	public ThenOrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByAscending, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
