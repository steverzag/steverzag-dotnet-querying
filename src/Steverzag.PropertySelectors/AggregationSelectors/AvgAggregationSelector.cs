using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.AvgAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record AvgAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => AvgAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.AvgAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record AvgAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.LiftedAggregateType(nameof(Enumerable.Average), ResultTypes.ElementOf(t)),
			_ => target => target!.CreateAggregateAvgFunction())
		{ Target = ArgumentConstraint.NumericCollection },
	];

	public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
