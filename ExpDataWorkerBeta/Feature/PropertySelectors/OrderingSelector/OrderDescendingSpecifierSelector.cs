using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.OrderingSelector
{
	public record OrderDescendingSpecifierSelector() : SpecifierSelector(SelectorDefaults.OrderDescending)
	{
	}
}
