using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.MinAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record MinAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MinAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.MinAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record MinAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.LiftedAggregateType(nameof(Enumerable.Min), ResultTypes.ElementOf(t)),
			_ => target => target!.CreateAggregateMinFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
