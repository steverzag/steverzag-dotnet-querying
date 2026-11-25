namespace Steverzag.PropertySelectors.ElementSelectors
{
	public abstract record ElementFunctionSelector<T> : FunctionPropertySelector<T>
	{
		protected ElementFunctionSelector(string PlainValue, List<PropertySelector>? parameters = null) : base(PlainValue, parameters)
		{
		}
	}
}
