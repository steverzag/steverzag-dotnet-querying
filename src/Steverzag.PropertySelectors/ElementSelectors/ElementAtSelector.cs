using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.ElementSelectors
{
	[SelectorToken(SelectorDefaults.ElementAtIndex)]
	public record ElementAtSelector<T> : FunctionPropertySelector<T>
	{

		public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex)
		{
		}

		public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 1)
			{
				// Overload with one parameter => ElementAt(number)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};

				var indexParameterFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, (target) => target.CreateElementAtFunction(Parameters[0]));
				Overloads.Add(indexParameterFunctionParameters);
			}
		}
	}

	[SelectorToken(SelectorDefaults.ElementAtIndex)]
	public record ElementAtSelector : FunctionPropertySelector
	{
		public ElementAtSelector() : base(SelectorDefaults.ElementAtIndex)
		{
		}
		public ElementAtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementAtIndex, parameters)
		{
		}
		public override void InitializeOverloads()
		{
			if (Parameters.Count == 1)
			{
				// Overload with one parameter => ElementAt(number)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType }
				};
				var indexParameterFunctionParameters = new FunctionPropertySelectorOverload(valueParameters, (target) => target.CreateElementAtFunction(Parameters[0]));
				Overloads.Add(indexParameterFunctionParameters);
			}
		}
	}
}
