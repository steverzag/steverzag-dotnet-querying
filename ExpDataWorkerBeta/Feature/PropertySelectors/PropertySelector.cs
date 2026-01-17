using ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors;
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
				var isFunctionPart = selectorPart.StartsWith(SelectorDefaults.FunctionStart);

				PropertySelector propertySelector;

				if (isFunctionPart)
				{
					propertySelector = selectorPart switch
					{
						SelectorDefaults.CountAggregateFunction => new AggregationSelectors.CountAggregationSelector(),
						SelectorDefaults.SumAggregateFunction => new AggregationSelectors.SumAggregationSelector(),
						SelectorDefaults.AvgAggregateFunction => new AggregationSelectors.AvgAggregationSelector(),
						SelectorDefaults.MinAggregateFunction => new AggregationSelectors.MinAggregationSelector(),
						SelectorDefaults.MaxAggregateFunction => new AggregationSelectors.MaxAggregationSelector(),

						SelectorDefaults.ElementFirst => new FirstElementSelector(),
						SelectorDefaults.ElementLast => new LastElementSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementAtIndex) =>
							new ElementAtSelector(),

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
	}
}
