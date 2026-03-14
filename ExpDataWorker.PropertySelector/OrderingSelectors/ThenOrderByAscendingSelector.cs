namespace ExpDataWorker.PropertySelector.OrderingSelectors
{
	public record ThenOrderByAscendingSelector : FunctionPropertySelector
	{
		public ThenOrderByAscendingSelector() : base(SelectorDefaults.OrderAscending)
		{
		}

		public ThenOrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderAscending, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 0)
			{
				// Overload with no parameters => OrderBy()
				var emptyFunctionParameters = new FunctionPropertySelectorOverload([], target => target.CreateThenOrderByAscendingFunction());
				Overloads.Add(emptyFunctionParameters);
			}
			else if (Parameters.Count == 1)
			{
				// Overload with one parameters => OrderBy(property_selector)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};

				var valueFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, target => target.CreateThenOrderByAscendingFunction(Parameters[0]));
				Overloads.Add(valueFunctionParameters);
			}
		}
	}
}
