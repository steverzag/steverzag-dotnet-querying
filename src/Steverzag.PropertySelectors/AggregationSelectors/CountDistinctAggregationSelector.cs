using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.CountDistinctAggregateFunction)]
public record CountDistinctAggregationSelector<T> : FunctionPropertySelector<T>
{
	public CountDistinctAggregationSelector() : base(SelectorDefaults.CountDistinctAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => CountDistinctAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.CountDistinctAggregateFunction)]
public record CountDistinctAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(int)),
			_ => target => target!.CreateAggregateCountDistinctFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public CountDistinctAggregationSelector() : base(SelectorDefaults.CountDistinctAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
