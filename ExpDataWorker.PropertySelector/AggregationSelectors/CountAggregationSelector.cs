using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record CountAggregationSelector : FunctionPropertySelector
	{
		public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Count()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateCountFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
