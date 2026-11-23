using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors
{
	[SelectorToken(SelectorDefaults.AvgAggregateFunction)]
	public record AvgAggregationSelector<T> : FunctionPropertySelector<T>
	{
		public override Type? SelectorType => typeof(double);//TODO: when enumerable is of decimal/float type. should be retrned decimal/float?

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
	[SelectorToken(SelectorDefaults.AvgAggregateFunction)]
	public record AvgAggregationSelector : FunctionPropertySelector
	{
		public override Type? SelectorType => typeof(double);

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
