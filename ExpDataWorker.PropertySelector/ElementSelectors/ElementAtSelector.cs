using ExpDataWorker.PropertySelector;
using ExpDataWorker.PropertySelector.AggregationSelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.ElementSelectors
{
	public record ElementAtSelector : FunctionPropertySelector
	{

		public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex)
		{
		}

		public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 1)
			{
				// Overload with one parameter => ElementAt(number)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};

				var indexParameterFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, (target) => target.CreateElemeentAtFunction(Parameters[0]));
				Overloads.Add(indexParameterFunctionParameters);
			}
		}
	}
}
