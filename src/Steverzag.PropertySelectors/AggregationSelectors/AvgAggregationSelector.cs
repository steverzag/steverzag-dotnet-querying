using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.AvgAggregateFunction)]
public record AvgAggregationSelector<T> : FunctionPropertySelector<T>
{
	public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => AvgAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.AvgAggregateFunction)]
public record AvgAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.LiftedAggregateType(nameof(Enumerable.Average), ResultTypes.ElementOf(t)),
			_ => target => target!.CreateAggregateAvgFunction())
		{ Target = ArgumentConstraint.NumericCollection },
	];

	public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
