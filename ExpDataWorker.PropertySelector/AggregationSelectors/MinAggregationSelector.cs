using ExpDataWorker.PropertySelector.AggregationSelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record MinAggregationSelector : FunctionPropertySelector
	{
		public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Min()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateMinFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
