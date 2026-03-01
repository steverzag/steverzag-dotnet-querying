using ExpDataWorker.PropertySelector.AggregationSelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record MinAggregationSelector : PropertySelector
	{
		public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction)
		{
			CreateAccessor = (target) => target.CreateAggregateMinFunction();
		}
	}
}
