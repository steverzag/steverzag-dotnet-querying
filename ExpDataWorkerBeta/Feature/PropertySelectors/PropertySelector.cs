using System.Text;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public record PropertySelector(string PlainValue)
	{
		private const char PropertyDelimiter = '|';
		public string PlainValue { get; init; } = PlainValue;
		public PropertySelector? ChildSelector { get; set; }

		public static implicit operator PropertySelector(string selector)
		{
			var selectorParts = selector.Split(PropertyDelimiter);

			PropertySelector result = null!;

			foreach (var selectorPart in selectorParts)
			{
				var isFunctionPart = selectorPart.StartsWith(FunctionDefaults.FunctionStart);
				PropertySelector propertySelector;

				if (isFunctionPart)
				{
					propertySelector = selectorPart switch
					{
						FunctionDefaults.CountAggregateFunction => new AggregationSelectors.CountAggregationSelector(),
						FunctionDefaults.SumAggregateFunction => new AggregationSelectors.SumAggregationSelector(),
						FunctionDefaults.AvgAggregateFunction => new AggregationSelectors.AvgAggregationSelector(),
						FunctionDefaults.MinAggregateFunction => new AggregationSelectors.MinAggregationSelector(),
						FunctionDefaults.MaxAggregateFunction => new AggregationSelectors.MaxAggregationSelector(),
						_ => throw new NotSupportedException($"The function '{selectorPart}' is not supported.")
					};
				}
				else
				{
					propertySelector = new PropertySelector(selectorPart);
				}

				if (result is null)
					result = propertySelector;
				else
				{ 	
					var current = result;
					while (current.ChildSelector is not null)
					{
						current = current.ChildSelector;
					}
					current.ChildSelector = propertySelector;
				}
			}

			return result;
		}

		//public PropertySelector(string plainValue)
		//{
		//	PlainValue = plainValue;
		//}
	}
}
