using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.ElementSelectors
{
	public record FirstElementSelector : PropertySelector
	{
		public FirstElementSelector() : base(SelectorDefaults.ElementFirst)
		{
			CreateAccessor = (target) => target.CreateElementFirstOrDefaultFunction();
		}
	}
}
