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

		// Enumerable.Average's own return type: double for int/long/double, float for float, decimal for decimal,
		// and the nullable form for nullable elements.
		public override Type? InitSelectorTypeToApply()
		{
			var elementType = ((IEnumerableFunctionSelector<T>)this).ValidateAndGetEnumerableType(nameof(AvgAggregationSelector));
			return Implementations.GetAggregateMethod(nameof(Enumerable.Average), elementType).ReturnType;
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
