using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record MaxAggregationSelector : FunctionPropertySelector
	{
		public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Max()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateMaxFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
