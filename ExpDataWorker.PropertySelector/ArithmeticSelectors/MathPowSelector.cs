
namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
{
	internal record MathPowSelector : FunctionPropertySelector
	{
		public MathPowSelector() : base(SelectorDefaults.MathPower)
		{
		}
		public MathPowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathPower, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			if (Parameters.Count == 2)
			{
				// Overload with two parameter => ElementAt(number)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType },
					{ 1, valueParamType }
				};

				var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, (target) => target.CreatePowFunction(Parameters[0], Parameters[1]));
				Overloads.Add(selectorOverload);
			}
		}
	}
}
