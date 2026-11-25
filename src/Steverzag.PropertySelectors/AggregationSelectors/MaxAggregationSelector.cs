using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.MaxAggregateFunction)]
internal record MaxAggregationSelector<T> : FunctionPropertySelector<T>
{
	public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MaxAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.MaxAggregateFunction)]
public record MaxAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.LiftedAggregateType(nameof(Enumerable.Max), ResultTypes.ElementOf(t)),
			_ => target => target!.CreateAggregateMaxFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
