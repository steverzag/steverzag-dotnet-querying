using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.SumAggregateFunction)]
public record SumAggregationSelector<T> : FunctionPropertySelector<T>
{
	public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => SumAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.SumAggregateFunction)]
public record SumAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => ResultTypes.ElementOf(t),
			_ => target => target!.CreateAggregateSumFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
