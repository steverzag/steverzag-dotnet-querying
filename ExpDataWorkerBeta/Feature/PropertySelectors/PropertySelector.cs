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
				var isSpecifierPart = selectorPart.StartsWith(SelectorDefaults.SpecifierStart);

				PropertySelector propertySelector;

				if (isFunctionPart)
				{
					var parameters = TryGetkParameters(selectorPart);
					var isParameteredFunction = parameters.Count > 0;

					propertySelector = selectorPart switch
					{
						SelectorDefaults.CountAggregateFunction => new AggregationSelectors.CountAggregationSelector(),
						SelectorDefaults.SumAggregateFunction => new AggregationSelectors.SumAggregationSelector(),
						SelectorDefaults.AvgAggregateFunction => new AggregationSelectors.AvgAggregationSelector(),
						SelectorDefaults.MinAggregateFunction => new AggregationSelectors.MinAggregationSelector(),
						SelectorDefaults.MaxAggregateFunction => new AggregationSelectors.MaxAggregationSelector(),

						SelectorDefaults.ElementFirst => new FirstElementSelector(),
						SelectorDefaults.ElementLast => new LastElementSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementLast) && isParameteredFunction =>
							new LastElementSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementAtIndex) =>
							new ElementAtSelector(),

						_ => throw new NotSupportedException($"The function '{selectorPart}' is not supported.")
					};
				}
				else if (isSpecifierPart)
				{
					propertySelector = selectorPart switch
					{
						SelectorDefaults.OrderAscending => new OrderingSelector.OrderAscendingSpecifierSelector(),
						SelectorDefaults.OrderDescending => new OrderingSelector.OrderDescendingSpecifierSelector(),
						_ => throw new NotSupportedException($"The specifier '{selectorPart}' is not supported.")
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

		private static List<PropertySelector> TryGetkParameters(string selector)
		{
			var startingParenthesesIndex = selector.IndexOf('(');
			if (startingParenthesesIndex is -1)
				return Enumerable.Empty<PropertySelector>().ToList();

			var result = new List<PropertySelector>();
			var endingParenthesesIndex = selector.LastIndexOf(')');

			var innerParameters = selector.Substring(startingParenthesesIndex + 1,
				endingParenthesesIndex - startingParenthesesIndex - 1);

			var nestedParenthesesCount = 0;
			var lastParameterEndIndex = 0;
			for (var i = 0; i < innerParameters.Length; i++)
			{
				var ch = innerParameters[i];
				if (ch == '(')
				{
					nestedParenthesesCount++;
					continue;
				}
				else if (ch == ')')
				{
					nestedParenthesesCount--;
					continue;
				}
				else if (nestedParenthesesCount == 0 && ch == ',')
				{
					var startIndex = lastParameterEndIndex is 0 
						? 0 
						: lastParameterEndIndex + 1;

					var parameter = innerParameters.Substring(startIndex, i - startIndex).Trim();
					if (!string.IsNullOrEmpty(parameter))
					{
						var propertySelector = (PropertySelector)parameter;
						result.Add(propertySelector);
					}
				}
				else if(nestedParenthesesCount == 0 && i == innerParameters.Length - 1)
				{
					var parameter = innerParameters.Substring(lastParameterEndIndex).Trim();
					if (!string.IsNullOrEmpty(parameter))
					{
						var propertySelector = (PropertySelector)parameter;
						result.Add(propertySelector);
					}
				}
			}
			return result;
		}
	}
}
