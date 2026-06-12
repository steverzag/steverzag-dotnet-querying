
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
{
	internal record MathPowSelector<T> : FunctionPropertySelector<T>
	{
		public override Type? SelectorType => typeof(double);
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
				// Overload with two parameter => Max(number1, number2)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType },
					{ 1, valueParamType }
				};
				var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
					target => target.CreateBinaryMathFunction(MethodInfos.MathPowerMethodInfo, Parameters[0], Parameters[1]));
				Overloads.Add(selectorOverload);
			}
		}
	}

	internal record MathPowSelector : FunctionPropertySelector
	{
		public override Type? SelectorType => typeof(double);
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
				// Overload with two parameter => Max(number1, number2)
				var valueParamType = typeof(PropertySelector);
				var valueParameters = new SortedList<int, Type>
				{
					{ 0, valueParamType },
					{ 1, valueParamType }
				};

				var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
					target => target.CreateBinaryMathFunction(MethodInfos.MathPowerMethodInfo, Parameters[0], Parameters[1]));
				Overloads.Add(selectorOverload);
			}
		}
	}
}
