using Steverzag.PropertySelectors.Extensions;
using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.ValueSelectors;
using System.Linq.Expressions;
using System.Text;

namespace Steverzag.PropertySelectors
{
	public record PropertySelector<T> : PropertySelector
	{
		public PropertySelector() : base()
		{
			SelectorType = InitSelectorTypeToApply();
		}
		public PropertySelector(string plainValue) : base(plainValue)
		{
			if (SelectorType is not null)
				return;

			SelectorType = InitSelectorTypeToApply();
		}

		public override Type? InitSelectorTypeToApply()
		{
			return typeof(T).GetPropertyType(this);
		}

		public static implicit operator PropertySelector<T>(string selector)
		{
			var analysis = selector.AnalyzePropertySelector();
			if (analysis.IsTopLevelArithmetic)
			{
				return PropertySelectorUtils.CreateArithmeticOperationSelector<T>(selector);
			}

			var selectorParts = analysis.Parts;
			PropertySelector<T> result = null!;
			Type currentIterationType = typeof(T);

			foreach (var selectorPart in selectorParts)
			{
				var isFunctionPart = selectorPart.StartsWith(SelectorDefaults.FunctionStart);
				var isSpecifierPart = selectorPart.StartsWith(SelectorDefaults.SpecifierStart);

				PropertySelector propertySelector;

				if (isFunctionPart)
				{
					propertySelector = SelectorRegistry.TryResolveGeneric(selectorPart, currentIterationType, out var registeredSelector)
						? registeredSelector
						: throw new NotSupportedException($"The function '{selectorPart}' is not supported.");
				}
				else if (ValueSelectorFactory.TryCreate<T>(selectorPart, out var valueSelector))
				{
					propertySelector = valueSelector;
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
		public string PlainValue { get; init; } = string.Empty;
		public virtual Type? SelectorType { get; init; }
		public Type? Type
		{
			get
			{
				var t = SelectorType;
				var child = ChildSelector;
				while (child != null)
				{ 
					t = child.SelectorType;
					child = child.ChildSelector;
				}
				return t;
			}
		}
		public PropertySelector? ChildSelector { get; set; }
		public Func<Expression?, Expression> PropertyAccess { get; init; } = null!; // assigned by every PropertySelector constructor
		public abstract Type? InitSelectorTypeToApply();
	}

	public record PropertySelector : SelectorBase
	{
		public override Type? InitSelectorTypeToApply()
		{
			return SelectorType;
		}

		protected PropertySelector()
		{
			PlainValue = string.Empty;
			PropertyAccess = (target) => target ?? Expression.Constant(null);
		}

		public PropertySelector(string plainValue)
		{
			PlainValue = plainValue;
			PropertyAccess = (target) =>
			{
				var propertyNames = PlainValue.Split('.');

				return ExpressionUtils.CreateMemberAccessRec(target ?? throw new ArgumentNullException(nameof(target), $"Property path '{PlainValue}' needs a target expression."), propertyNames, 0);
			};
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(PlainValue);
			var child = ChildSelector;

			while (child != null)
			{
				sb.Append(SelectorDefaults.PropertyDelimiter);
				sb.Append(child.PlainValue);
				child = child.ChildSelector;
			}

			return sb.ToString();
		}

		public static implicit operator PropertySelector(string selector)
		{
			var analysis = selector.AnalyzePropertySelector();
			if (analysis.IsTopLevelArithmetic)
			{
				return PropertySelectorUtils.CreateArithmeticOperationSelector(selector);
			}
			var selectorParts = analysis.Parts;
			PropertySelector result = null!;

			foreach (var selectorPart in selectorParts)
			{
				var isFunctionPart = selectorPart.StartsWith(SelectorDefaults.FunctionStart);
				var isSpecifierPart = selectorPart.StartsWith(SelectorDefaults.SpecifierStart);

				PropertySelector propertySelector;

				if (isFunctionPart)
				{
					propertySelector = SelectorRegistry.TryResolveNonGeneric(selectorPart, out var registeredSelector)
						? registeredSelector
						: throw new NotSupportedException($"The function '{selectorPart}' is not supported.");
				}
				else if (isSpecifierPart)
				{
					propertySelector = selectorPart switch
					{
						//SelectorDefaults.OrderDescending => new OrderBy(),
						_ => throw new NotSupportedException($"The specifier '{selectorPart}' is not supported.")
					};
				}
				else if (ValueSelectorFactory.TryCreate(selectorPart, out var valueSelector))
				{
					propertySelector = valueSelector;
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
