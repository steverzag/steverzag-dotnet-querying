using Steverzag.PropertySelectors.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.AggregationSelectors
{
	[SelectorToken(SelectorDefaults.MinAggregateFunction)]
	public record MinAggregationSelector<T> : FunctionPropertySelector<T>, IEnumerableFunctionSelector<T>
	{
		public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction)
		{
		}
		public override void InitializeOverloads()
		{
			// Overload with no parameters => Min()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateMinFunction());
			Overloads.Add(emptyFunctionParameters);
		}
		public override Type? InitSelectorTypeToApply()
		{
			return ((IEnumerableFunctionSelector<T>)this).ValidateAndGetEnumerableType(nameof(MinAggregationSelector));
		}
	}

	[SelectorToken(SelectorDefaults.MinAggregateFunction)]
	public record MinAggregationSelector : FunctionPropertySelector
	{
		public MinAggregationSelector() : base(SelectorDefaults.MinAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Min()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateMinFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
