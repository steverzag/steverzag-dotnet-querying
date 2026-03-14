using ExpDataWorker.PropertySelectors.OrderingSelectors;

namespace ExpDataWorker.PropertySelectors.OrderingSelectors
{
	public record ThenOrderByDescendingSelector : FunctionPropertySelector
	{
		public ThenOrderByDescendingSelector() : base(SelectorDefaults.OrderDescending)
		{
		}

		public ThenOrderByDescendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderDescending, parameters)
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
