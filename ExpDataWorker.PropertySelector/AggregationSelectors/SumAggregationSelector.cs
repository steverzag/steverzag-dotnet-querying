using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record SumAggregationSelector : PropertySelector
	{
		public SumAggregationSelector() : base(SelectorDefaults.SumAggregateFunction)
		{
			CreateAccessor = (target) => target.CreateAggregateSumFunction();
		}
	}
}
