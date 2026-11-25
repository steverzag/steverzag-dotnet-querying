using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.LongCountAggregateFunction)]
public record LongCountAggregationSelector<T> : FunctionPropertySelector<T>
{
	public LongCountAggregationSelector() : base(SelectorDefaults.LongCountAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => LongCountAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.LongCountAggregateFunction)]
public record LongCountAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(long)),
			_ => target => target!.CreateAggregateLongCountFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public LongCountAggregationSelector() : base(SelectorDefaults.LongCountAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
