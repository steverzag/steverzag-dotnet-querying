using ExpDataWorker.PropertySelectors.Registration;

namespace ExpDataWorker.PropertySelectors.ElementSelectors
{

	[SelectorToken(SelectorDefaults.ElementLast)]
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

	[SelectorToken(SelectorDefaults.ElementLast)]
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
