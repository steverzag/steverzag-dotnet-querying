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
	public record LastElementSelector : FunctionPropertySelector
	{
		public LastElementSelector() : base(SelectorDefaults.ElementLast)
		{
			//CreateAccessor = (target) => target.CreateElementLastOrDefaultFunction();
		}

		public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateElementLastOrDefaultFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
