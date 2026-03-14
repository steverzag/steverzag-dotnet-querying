using ExpDataWorker.PropertySelectors.OrderingSelectors;

namespace ExpDataWorker.PropertySelectors.OrderingSelectors
{
	public record OrderByDescendingSelector : FunctionPropertySelector
	{
		public OrderByDescendingSelector() : base(SelectorDefaults.OrderDescending)
		{
		}

		public OrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderDescending, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 0)
			{
				// Overload with no parameters => OrderByDescending()
				var emptyFunctionParameters = new FunctionPropertySelectorOverload([], target => target.CreateOrderByDescendingFunction());
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

				var valueFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, target => target.CreateOrderByDescendingFunction(Parameters[0]));
				Overloads.Add(valueFunctionParameters);
			}
		}
	}
}
