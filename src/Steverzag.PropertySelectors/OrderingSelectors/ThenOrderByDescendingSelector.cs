using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.OrderingSelectors;

namespace Steverzag.PropertySelectors.OrderingSelectors
{
	[SelectorToken(SelectorDefaults.ThenOrderByDescending, Arguments = ArgumentScope.TargetElement)]
	public record ThenOrderByDescendingSelector<T> : OrderingFunctionSelector<T>
	{
		public ThenOrderByDescendingSelector() : base(SelectorDefaults.ThenOrderByDescending)
		{
		}
		public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByDescending, parameters)
		{
		}
		public override void InitializeOverloads()
		{
			if (Parameters.Count == 0)
			{
				// Overload with no parameters => ThenOrderByDescending()
				var emptyFunctionParameters = new FunctionPropertySelectorOverload([], target => target.CreateThenOrderByDescendingFunction());
				Overloads.Add(emptyFunctionParameters);
			}
			else if (Parameters.Count == 1)
			{
				// Overload with one parameters => ThenOrderByDescending(property_selector)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};
				var valueFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, target => target.CreateThenOrderByDescendingFunction(Parameters[0]));
				Overloads.Add(valueFunctionParameters);
			}
		}
	}
	[SelectorToken(SelectorDefaults.ThenOrderByDescending, Arguments = ArgumentScope.TargetElement)]
	public record ThenOrderByDescendingSelector : FunctionPropertySelector
	{
		public ThenOrderByDescendingSelector() : base(SelectorDefaults.ThenOrderByDescending)
		{
		}

		public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ThenOrderByDescending, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 0)
			{
				// Overload with no parameters => OrderByDescending()
				var emptyFunctionParameters = new FunctionPropertySelectorOverload([], target => target.CreateThenOrderByDescendingFunction());
				Overloads.Add(emptyFunctionParameters);
			}
			else if (Parameters.Count == 1)
			{
				// Overload with one parameters => OrderByDescending(property_selector)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};

				var valueFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, target => target.CreateThenOrderByDescendingFunction(Parameters[0]));
				Overloads.Add(valueFunctionParameters);
			}
		}
	}
}
