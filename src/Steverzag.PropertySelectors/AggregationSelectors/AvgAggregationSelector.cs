using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors
{
	[SelectorToken(SelectorDefaults.AvgAggregateFunction)]
	public record AvgAggregationSelector<T> : FunctionPropertySelector<T>, IEnumerableFunctionSelector<T>
	{
		public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction)
		{
		}
		public override void InitializeOverloads()
		{
			// Overload with no parameters => Avg()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateAvgFunction());
			Overloads.Add(emptyFunctionParameters);
		}

		public override Type? InitSelectorTypeToApply()
		{
			var elementType = ((IEnumerableFunctionSelector<T>)this).ValidateAndGetEnumerableType(nameof(AvgAggregationSelector));
			return Implementations.LiftedAggregateType(nameof(Enumerable.Average), elementType);
		}
	}
	[SelectorToken(SelectorDefaults.AvgAggregateFunction)]
	public record AvgAggregationSelector : FunctionPropertySelector
	{
		public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Avg()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateAvgFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
