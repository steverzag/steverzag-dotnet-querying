using ExpDataWorker.PropertySelectors.ArithmeticSelectors;
using ExpDataWorker.PropertySelectors.ElementSelectors;
using ExpDataWorker.PropertySelectors.Extensions;
using ExpDataWorker.PropertySelectors.OrderingSelectors;
using ExpDataWorker.PropertySelectors.ValueSelectors;
using System.Linq.Expressions;

namespace ExpDataWorker.PropertySelectors
{
	public record PropertySelector<T> : PropertySelector
	{
		public Type RootType { get; } = typeof(T);
		public PropertySelector() : base()
		{
			SelectorType = InitSelectorTypeToApply();
		}
		public PropertySelector(string plainValue) : base(plainValue)
		{
			if(SelectorType is not null)
				return;

			SelectorType = InitSelectorTypeToApply();
		}

		public override Type? InitSelectorTypeToApply()
		{
			return RootType.GetPropertyType(this);
		}

		public static implicit operator PropertySelector<T>(string selector)
		{

			var selectorParts = selector.SplitPropertySelectors();
			PropertySelector<T> result = null!;
			Type currentIterationType = typeof(T);

			foreach (var selectorPart in selectorParts)
			{
				var isFunctionPart = selectorPart.StartsWith(SelectorDefaults.FunctionStart);
				var isSpecifierPart = selectorPart.StartsWith(SelectorDefaults.SpecifierStart);

				PropertySelector propertySelector;

				if (isFunctionPart)
				{
					propertySelector = selectorPart switch
					{
					//	SelectorDefaults.CountAggregateFunction => new AggregationSelectors.CountAggregationSelector(),
					//	SelectorDefaults.LongCountAggregateFunction => new AggregationSelectors.LongCountAggregationSelector(),
					//	SelectorDefaults.SumAggregateFunction => new AggregationSelectors.SumAggregationSelector(),
					//	SelectorDefaults.AvgAggregateFunction => new AggregationSelectors.AvgAggregationSelector(),
					//	SelectorDefaults.MinAggregateFunction => new AggregationSelectors.MinAggregationSelector(),
					//	SelectorDefaults.MaxAggregateFunction => new AggregationSelectors.MaxAggregationSelector(),

						//Oodering selectors
						SelectorDefaults.OrderAscending => GenericPropertySelectorFactory.CreateGeneric(typeof(OrderByAscendingSelector<>), currentIterationType),
						SelectorDefaults.OrderDescending => GenericPropertySelectorFactory.CreateGeneric(typeof(OrderByDescendingSelector<>), currentIterationType),
						SelectorDefaults.ThenOrderByAscending => GenericPropertySelectorFactory.CreateGeneric(typeof(ThenOrderByAscendingSelector<>), currentIterationType),
						SelectorDefaults.ThenOrderByDescending => GenericPropertySelectorFactory.CreateGeneric(typeof(ThenOrderByDescendingSelector<>), currentIterationType),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderAscending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							GenericPropertySelectorFactory.CreateGeneric(typeof(OrderByAscendingSelector<>), currentIterationType, parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderDescending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							GenericPropertySelectorFactory.CreateGeneric(typeof(OrderByDescendingSelector<>), currentIterationType, parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ThenOrderByAscending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						GenericPropertySelectorFactory.CreateGeneric(typeof(ThenOrderByAscendingSelector<>), currentIterationType, parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ThenOrderByDescending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							GenericPropertySelectorFactory.CreateGeneric(typeof(ThenOrderByDescendingSelector<>), currentIterationType, parameters),

						SelectorDefaults.ElementFirst => GenericPropertySelectorFactory.CreateGeneric(typeof(FirstElementSelector<>), currentIterationType),
						SelectorDefaults.ElementLast => GenericPropertySelectorFactory.CreateGeneric(typeof(LastElementSelector<>), currentIterationType),
						//_ when selectorPart.StartsWith(SelectorDefaults.ElementLast) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new LastElementSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementAtIndex) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							GenericPropertySelectorFactory.CreateGeneric(typeof(ElementAtSelector<>), currentIterationType, parameters),

						//_ when selectorPart.StartsWith(SelectorDefaults.MathPower) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathPowSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathCeiling) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathCeilingSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathFloor) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathFloorSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathRound) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathRoundSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathTruncate) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathTruncateSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathSign) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathSignSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathSqrt) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathSqrtSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathExp) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathExpSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathLog) && !selectorPart.StartsWith(SelectorDefaults.MathLog10) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathLogSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathLog10) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathLog10Selector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathSin) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathSinSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathCos) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathCosSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathTan) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathTanSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathAsin) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathAsinSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathAcos) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathAcosSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathAtan2) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathAtan2Selector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathAtan) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathAtanSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathMax) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathMaxSelector(parameters),
						//_ when selectorPart.StartsWith(SelectorDefaults.MathMin) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
						//	new MathMinSelector(parameters),

						_ => throw new NotSupportedException($"The function '{selectorPart}' is not supported.")
					};
				}
				else if (int.TryParse(selectorPart, out int intValue))
				{
					propertySelector = new IntegerValueSelector<T>(intValue);
				}
				else if (double.TryParse(selectorPart, out double doubleValue))
				{
					propertySelector = new DoubleValueSelector<T>(doubleValue);
				}
				else if (bool.TryParse(selectorPart, out bool boolValue))
				{
					propertySelector = new BooleanValueSelector<T>(boolValue);
				}
				else
				{
					propertySelector = new PropertySelector<T>(selectorPart);
				}

				if (result is null)
					result = (PropertySelector<T>)propertySelector;
				else
				{
					var current = (PropertySelector)result;
					while (current.ChildSelector is not null)
					{
						current = current.ChildSelector;
					}
					current.ChildSelector = propertySelector;
				}

				currentIterationType = propertySelector.SelectorType!;
			}

			return result;
		}
	}



	public abstract record SelectorBase
	{
		public string PlainValue { get; init; }
		public virtual Type? SelectorType { get; init; }
		public PropertySelector? ChildSelector { get; set; }
		public Func<Expression?, Expression> PropertyAccess { get; init; }
		public abstract Type? InitSelectorTypeToApply();
	}

	public record PropertySelector : SelectorBase
	{
		public override Type? InitSelectorTypeToApply()
		{
			return SelectorType;
		}

		protected PropertySelector() {
			PlainValue = string.Empty;
			PropertyAccess = (target) => target ?? Expression.Constant(null);
		}

		public PropertySelector(string plainValue)
		{
			PlainValue = plainValue;
			PropertyAccess = (target) =>
			{
				var propertyNames = PlainValue.Split('.');

				return ExpressionUtils.CreateMemberAccessRec(target, propertyNames, 0);
			};
		}

		public static implicit operator PropertySelector(string selector)
		{
			bool isArithmeticOperation = selector.IsArithmeticExpressionTopLevel();
			if (isArithmeticOperation)
			{ 
				return PropertySelectorUtils.CreateArithmeticOperationSelector(selector);
			}
				var selectorParts = selector.SplitPropertySelectors();
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
						SelectorDefaults.LongCountAggregateFunction => new AggregationSelectors.LongCountAggregationSelector(),
						SelectorDefaults.SumAggregateFunction => new AggregationSelectors.SumAggregationSelector(),
						SelectorDefaults.AvgAggregateFunction => new AggregationSelectors.AvgAggregationSelector(),
						SelectorDefaults.MinAggregateFunction => new AggregationSelectors.MinAggregationSelector(),
						SelectorDefaults.MaxAggregateFunction => new AggregationSelectors.MaxAggregationSelector(),

						SelectorDefaults.OrderAscending => new OrderByAscendingSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderAscending) && !PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new OrderByAscendingSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderAscending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new OrderByAscendingSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.OrderDescending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new OrderByDescendingSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ThenOrderByAscending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new ThenOrderByAscendingSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ThenOrderByDescending) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new ThenOrderByDescendingSelector(parameters),

						SelectorDefaults.ElementFirst => new FirstElementSelector(),
						SelectorDefaults.ElementLast => new LastElementSelector(),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementLast) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new LastElementSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.ElementAtIndex) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new ElementAtSelector(parameters),

						_ when selectorPart.StartsWith(SelectorDefaults.MathPower) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathPowSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathCeiling) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathCeilingSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathFloor) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathFloorSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathRound) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathRoundSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathTruncate) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathTruncateSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathSign) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathSignSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathSqrt) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathSqrtSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathExp) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathExpSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathLog) && !selectorPart.StartsWith(SelectorDefaults.MathLog10) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathLogSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathLog10) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathLog10Selector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathSin) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathSinSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathCos) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathCosSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathTan) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathTanSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathAsin) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathAsinSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathAcos) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathAcosSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathAtan2) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathAtan2Selector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathAtan) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathAtanSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathMax) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathMaxSelector(parameters),
						_ when selectorPart.StartsWith(SelectorDefaults.MathMin) && PropertySelectorUtils.TryGetParameters(selectorPart, out List<PropertySelector> parameters) =>
							new MathMinSelector(parameters),

						_ => throw new NotSupportedException($"The function '{selectorPart}' is not supported.")
					};
				}
				else if (isSpecifierPart)
				{
					propertySelector = selectorPart switch
					{
						//SelectorDefaults.OrderDescending => new OrderBy(),
						_ => throw new NotSupportedException($"The specifier '{selectorPart}' is not supported.")
					};
				}
				else if (int.TryParse(selectorPart, out int intValue))
				{
					propertySelector = new IntegerValueSelector(intValue);
				}
				else if (double.TryParse(selectorPart, out double doubleValue))
				{
					propertySelector = new DoubleValueSelector(doubleValue);
				}
				else if (bool.TryParse(selectorPart, out bool boolValue))
				{
					propertySelector = new BooleanValueSelector(boolValue);
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
