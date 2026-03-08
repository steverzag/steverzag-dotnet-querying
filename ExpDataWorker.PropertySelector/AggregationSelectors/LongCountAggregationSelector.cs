using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record LongCountAggregationSelector : FunctionPropertySelector
	{
		public LongCountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => LongCount()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateLongCountFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
