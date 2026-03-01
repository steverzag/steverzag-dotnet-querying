using ExpDataWorker.PropertySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.ElementSelectors
{
	public record ElementAtSelector() : PropertySelector(SelectorDefaults.ElementAtIndex)
	{

	}
}
