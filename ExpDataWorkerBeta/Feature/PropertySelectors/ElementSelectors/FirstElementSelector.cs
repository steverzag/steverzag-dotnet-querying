using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors
{
	public record FirstElementSelector() : PropertySelector(SelectorDefaults.ElementFirst)
	{

	}
}
