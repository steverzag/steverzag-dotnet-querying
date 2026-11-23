using Steverzag.PropertySelectors.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.AggregationSelectors
{

	[SelectorToken(SelectorDefaults.CountAggregateFunction)]
	public record CountAggregationSelector<T> : FunctionPropertySelector<T>, IEnumerableFunctionSelector<T>
	{
		public override Type? SelectorType => typeof(int);

		public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction)
		{
			((IEnumerableFunctionSelector<T>)this).ValidateEnumerable(nameof(CountAggregationSelector));
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Count()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateCountFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}

	[SelectorToken(SelectorDefaults.CountAggregateFunction)]
	public record CountAggregationSelector : FunctionPropertySelector
	{
		public override Type? SelectorType => typeof(int);

		public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Count()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateCountFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
