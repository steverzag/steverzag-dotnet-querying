namespace ExpDataWorker.PropertySelectors.ElementSelectors
{

	public record LastElementSelector<T> : ElementFunctionSelector<T>
	{
		public LastElementSelector() : base(SelectorDefaults.ElementLast)
		{
		}

		public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Last()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateElementLastOrDefaultFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}

	public record LastElementSelector : FunctionPropertySelector
	{
		public LastElementSelector() : base(SelectorDefaults.ElementLast)
		{
		}

		public LastElementSelector(List<PropertySelector> parameters) : base(SelectorDefaults.ElementLast, parameters)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Last()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateElementLastOrDefaultFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
