namespace ExpDataWorker.PropertySelector.OrderingSelectors
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
				var emptyFunctionParameters = new FunctionPropertySelectorOverload([], target => target.CreateOrderByDescendingFunction());
				Overloads.Add(emptyFunctionParameters);
			}
			else if (Parameters.Count == 1)
			{
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
