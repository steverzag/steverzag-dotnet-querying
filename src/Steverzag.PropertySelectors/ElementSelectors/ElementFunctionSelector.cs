namespace Steverzag.PropertySelectors.ElementSelectors
{
	internal abstract record ElementFunctionSelector<T> : FunctionPropertySelector<T>
	{
		protected ElementFunctionSelector(string PlainValue, List<PropertySelector>? parameters = null) : base(PlainValue, parameters)
		{
		}
	}
}
