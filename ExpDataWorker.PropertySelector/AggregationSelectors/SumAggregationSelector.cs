using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.AggregationSelectors
{
	public record SumAggregationSelector<T> : FunctionPropertySelector<T>, IEnumerableFunctionSelector<T>
	{
		public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction)
		{
		}
		public override void InitializeOverloads()
		{
			// Overload with no parameters => Sum()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateSumFunction());
			Overloads.Add(emptyFunctionParameters);
		}
		public override Type? InitSelectorTypeToApply()
		{
			return ((IEnumerableFunctionSelector<T>)this).ValidateAndGetEnumerableType(nameof(SumAggregationSelector));
		}
	}

	public record SumAggregationSelector : FunctionPropertySelector
	{
		public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Sum()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateSumFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
