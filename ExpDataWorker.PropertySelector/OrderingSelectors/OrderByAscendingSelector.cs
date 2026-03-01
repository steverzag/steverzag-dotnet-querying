namespace ExpDataWorker.PropertySelector.OrderingSelectors
{
	public record OrderByAscendingSelector : FunctionPropertySelector
	{
		public OrderByAscendingSelector() : base(SelectorDefaults.OrderAscending)
		{
		}

		public OrderByAscendingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.OrderAscending, parameters)
		{
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
