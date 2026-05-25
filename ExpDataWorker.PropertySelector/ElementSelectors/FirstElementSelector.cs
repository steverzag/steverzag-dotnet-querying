using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ElementSelectors
{
	public record FirstElementSelector <T>: ElementFunctionSelector<T>
	{
		public FirstElementSelector() : base(SelectorDefaults.ElementFirst)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => First()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateElementFirstOrDefaultFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
	public record FirstElementSelector : FunctionPropertySelector
	{
		public FirstElementSelector() : base(SelectorDefaults.ElementFirst)
		{
		}

		public FirstElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementFirst, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => First()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateElementFirstOrDefaultFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}