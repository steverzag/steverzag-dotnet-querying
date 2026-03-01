using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record MaxAggregationSelector : PropertySelector
	{
		public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction)
		{
			CreateAccessor = (target) => target.CreateAggregateMaxFunction();
		}
	}
}
