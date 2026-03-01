using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.AggregationSelectors
{
	public record CountAggregationSelector : PropertySelector
	{
		public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction)
		{
			CreateAccessor = (target) => target.CreateAggregateCountFunction();
		}
	}
}
