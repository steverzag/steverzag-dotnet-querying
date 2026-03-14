using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.AggregationSelectors
{
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
