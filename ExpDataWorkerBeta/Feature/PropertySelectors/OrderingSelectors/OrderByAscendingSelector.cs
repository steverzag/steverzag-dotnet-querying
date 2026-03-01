using ExpDataWorkerBeta.Feature.PropertySelectors.SpecifierTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.OrderingSelectors
{
	public record OrderByAscendingSelector : FunctionPropertySelector
	{
		public OrderByAscendingSelector() : base(SelectorDefaults.OrderAscending)
		{
			//CreateAccessor = (target) => target.CreateOrderByAscendingFunction(this);
		}

		public OrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderAscending, parameters)
		{
			//CreateAccessor = (target) => target.CreateOrderByAscendingFunction(this);
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 0)
			{
				var emptyFunctionParameters = new FunctionPropertySelectorOverload([], target => target.CreateOrderByAscendingFunction());
				Overloads.Add(emptyFunctionParameters);
			}
			else if (Parameters.Count == 1)
			{
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};

				var valueFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, target => target.CreateOrderByAscendingFunction(Parameters[0]));
				Overloads.Add(valueFunctionParameters);
			}
		}
	}
}
