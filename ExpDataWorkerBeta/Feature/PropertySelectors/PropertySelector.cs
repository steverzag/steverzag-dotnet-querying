using ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors;
using ExpDataWorkerBeta.Feature.PropertySelectors.OrderingSelectors;
using ExpDataWorkerBeta.Feature.Utils;
using System.Linq.Expressions;
using System.Text;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public record PropertySelector
	{
		private const char PropertyDelimiter = '|';
		public string PlainValue { get; init; }
		public PropertySelector? ChildSelector { get; set; }

		public virtual Func<Expression, Expression> CreateAccessor { get; init; }

		public PropertySelector(string plainValue)
		{
			PlainValue = plainValue;
			CreateAccessor = (Expression target) =>
			{
				var propertyNames = PlainValue.Split('.');

				return ExpressionUtils.CreateMemberAccessRec(target, propertyNames, 0);
			};
		}

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
					propertySelector = selectorPart switch
					{
						SelectorDefaults.CountAggregateFunction => new AggregationSelectors.CountAggregationSelector(),
						SelectorDefaults.SumAggregateFunction => new AggregationSelectors.SumAggregationSelector(),
						SelectorDefaults.AvgAggregateFunction => new AggregationSelectors.AvgAggregationSelector(),
						SelectorDefaults.MinAggregateFunction => new AggregationSelectors.MinAggregationSelector(),
						SelectorDefaults.MaxAggregateFunction => new AggregationSelectors.MaxAggregationSelector(),

						SelectorDefaults.ElementFirst => new FirstElementSelector(),
						SelectorDefaults.ElementLast => new LastElementSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementLast) && TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
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
						SelectorDefaults.OrderAscending => new OrderByAscendingSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderAscending) && !TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new OrderByAscendingSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderAscending) && TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new OrderByAscendingSelector(parameters),
						//SelectorDefaults.OrderDescending => new OrderBy(),
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

		private static bool TryGetParameters(string selector, out List<PropertySelector> parameters)
		{
			var startingParenthesesIndex = selector.IndexOf('(');
			if (startingParenthesesIndex is -1) { 
				parameters = [];
				return false;
			}

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

			parameters = result;

			return parameters.Count > 0;
		}
	}
}
