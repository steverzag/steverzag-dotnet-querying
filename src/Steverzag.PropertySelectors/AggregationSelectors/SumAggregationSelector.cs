using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.SumAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record SumAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => SumAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.SumAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record SumAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => ResultTypes.ElementOf(t),
			_ => target => target!.CreateAggregateSumFunction())
		{ Target = ArgumentConstraint.NumericCollection },
	];

	public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
