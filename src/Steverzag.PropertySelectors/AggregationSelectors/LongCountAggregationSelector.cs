using Steverzag.PropertySelectors.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.AggregationSelectors
{

	[SelectorToken(SelectorDefaults.LongCountAggregateFunction)]
	public record LongCountAggregationSelector<T> : FunctionPropertySelector<T>, IEnumerableFunctionSelector<T>
	{
		public override Type? SelectorType => typeof(long);
		public LongCountAggregationSelector() : base(SelectorDefaults.LongCountAggregateFunction)
		{
			((IEnumerableFunctionSelector<T>)this).ValidateEnumerable(nameof(LongCountAggregationSelector));
		}
		public override void InitializeOverloads()
		{
			// Overload with no parameters => LongCount()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateLongCountFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}

	[SelectorToken(SelectorDefaults.LongCountAggregateFunction)]
	public record LongCountAggregationSelector : FunctionPropertySelector
	{
		public override Type? SelectorType => typeof(long);
		public LongCountAggregationSelector() : base(SelectorDefaults.LongCountAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => LongCount()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateLongCountFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
