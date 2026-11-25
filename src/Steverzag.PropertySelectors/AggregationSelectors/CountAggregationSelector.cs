using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.CountAggregateFunction)]
public record CountAggregationSelector<T> : FunctionPropertySelector<T>
{
	public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => CountAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.CountAggregateFunction)]
public record CountAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(int)),
			_ => target => target!.CreateAggregateCountFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
