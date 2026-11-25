namespace Steverzag.PropertySelectors.OrderingSelectors
{
	public abstract record OrderingFunctionSelector<T> : FunctionPropertySelector<T>
	{
		protected OrderingFunctionSelector(string PlainValue, List<PropertySelector>? parameters = null) : base(PlainValue, parameters)
		{
		}
	}
}
