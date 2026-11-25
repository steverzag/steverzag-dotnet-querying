using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.MinAggregateFunction)]
public record MinAggregationSelector<T> : FunctionPropertySelector<T>
{
	public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MinAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.MinAggregateFunction)]
public record MinAggregationSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.LiftedAggregateType(nameof(Enumerable.Min), ResultTypes.ElementOf(t)),
			_ => target => target!.CreateAggregateMinFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
