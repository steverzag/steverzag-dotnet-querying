using Steverzag.PropertySelectors.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.AggregationSelectors
{
	[SelectorToken(SelectorDefaults.MaxAggregateFunction)]
	internal record MaxAggregationSelector<T> : FunctionPropertySelector<T>, IEnumerableFunctionSelector<T>
	{
		public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction)
		{
		}
		public override void InitializeOverloads()
		{
			// Overload with no parameters => Max()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateMaxFunction());
			Overloads.Add(emptyFunctionParameters);
		}

		public override Type? InitSelectorTypeToApply()
		{
			var elementType = ((IEnumerableFunctionSelector<T>)this).ValidateAndGetEnumerableType(nameof(MaxAggregationSelector));
			return Implementations.LiftedAggregateType(nameof(Enumerable.Max), elementType);
		}
	}
	[SelectorToken(SelectorDefaults.MaxAggregateFunction)]
	public record MaxAggregationSelector : FunctionPropertySelector
	{
		public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Max()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateMaxFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
