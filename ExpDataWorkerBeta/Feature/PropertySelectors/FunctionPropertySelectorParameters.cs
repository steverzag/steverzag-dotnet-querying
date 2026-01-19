using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public record FunctionPropertySelectorParameters(SortedList<int, Type> Parameters)
	{
	}
}
