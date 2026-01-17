namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public record ParameterizedFunctionPropertySelector(string PlainValue) : PropertySelector(PlainValue)
	{
	}
}
