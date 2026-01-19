using ExpDataWorkerBeta.Feature.Ordering;
using ExpDataWorkerBeta.Feature.PropertySelectors.SpecifierTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors
{
	public record LastElementSelector : ParameterizedFunctionPropertySelector
	{
		public LastElementSelector() : base(SelectorDefaults.ElementLast)
		{
		}

		public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			var emptyParameters = new SortedList<int, Type>();
			var emptyFunctionParameters = new FunctionPropertySelectorParameters(emptyParameters);
			Overloads.Add(emptyFunctionParameters);

			var orderParamType = typeof(OrderSpecifiers);
			var orderParameters = new SortedList<int, Type>
			{
				{ 0, orderParamType }
			};
			var orderFunctionParameters = new FunctionPropertySelectorParameters(orderParameters);
			Overloads.Add(orderFunctionParameters);
		}
	}
}
