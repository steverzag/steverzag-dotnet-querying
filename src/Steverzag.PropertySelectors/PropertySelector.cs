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
			=> SelectorLimits.Parse(selector, ParseText);

		private static PropertySelector<T> ParseText(string selector)
		{
			var analysis = selector.AnalyzePropertySelector();
			if (analysis.IsTopLevelArithmetic)
			{
				return (PropertySelector<T>)PropertySelectorUtils.ParseArithmetic(selector, () => PropertySelectorUtils.CreateArithmeticOperationSelector<T>(selector));
			}

			return Chain(selector, analysis.Parts, null, null);
		}

		internal static PropertySelector<T> Chain(string selector, IEnumerable<SelectorPartText> selectorParts, PropertySelector<T>? head, string? pipedText)
		{
			PropertySelector<T> result = head!;
			Type currentIterationType = head is null ? typeof(T) : head.Type!;

			foreach (var (selectorPart, offset) in selectorParts)
			{
				var isFunctionPart = selectorPart.StartsWith(SelectorDefaults.ComplexSelectorStart);

				var propertySelector = PropertySelectorUtils.ParsePart(selector, selectorPart, offset, () =>
				{
					if (isFunctionPart)
					{
						return SelectorRegistry.TryResolveGeneric(selectorPart, currentIterationType, typeof(T), pipedText, out var registeredSelector)
							? registeredSelector
							: throw PropertySelectorUtils.UnknownFunction(selectorPart);
					}

					if (ValueSelectorFactory.TryCreate<T>(selectorPart, out var valueSelector))
						return result is null ? valueSelector : throw PropertySelectorUtils.LiteralAfterPipe(selectorPart);

					PropertySelectorUtils.ValidatePropertyPath(selectorPart);
					return PropertySelectorUtils.CreatePropertySegment(selectorPart, currentIterationType);
				});

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
				pipedText = pipedText is null ? selectorPart : pipedText + SelectorDefaults.PropertyDelimiter + selectorPart;
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

		public sealed override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(SegmentToString());
			var child = ChildSelector;

			while (child != null)
			{
				sb.Append(SelectorDefaults.PropertyDelimiter);
				sb.Append(child.SegmentToString());
				child = child.ChildSelector;
			}

			return sb.ToString();
		}

		protected virtual string SegmentToString() => PlainValue;

		public static implicit operator PropertySelector(string selector)
			=> SelectorLimits.Parse(selector, ParseText);

		private static PropertySelector ParseText(string selector)
		{
			var analysis = selector.AnalyzePropertySelector();
			if (analysis.IsTopLevelArithmetic)
			{
				return PropertySelectorUtils.ParseArithmetic(selector, () => PropertySelectorUtils.CreateArithmeticOperationSelector(selector));
			}
			return Chain(selector, analysis.Parts, null, null);
		}

		internal static PropertySelector Chain(string selector, IEnumerable<SelectorPartText> selectorParts, PropertySelector? head, string? pipedText)
		{
			PropertySelector result = head!;

			foreach (var (selectorPart, offset) in selectorParts)
			{
				var isFunctionPart = selectorPart.StartsWith(SelectorDefaults.ComplexSelectorStart);

				var propertySelector = PropertySelectorUtils.ParsePart(selector, selectorPart, offset, () =>
				{
					if (isFunctionPart)
					{
						return SelectorRegistry.TryResolveNonGeneric(selectorPart, pipedText, out var registeredSelector)
							? registeredSelector
							: throw PropertySelectorUtils.UnknownFunction(selectorPart);
					}

					if (ValueSelectorFactory.TryCreate(selectorPart, out var valueSelector))
						return result is null ? valueSelector : throw PropertySelectorUtils.LiteralAfterPipe(selectorPart);

					PropertySelectorUtils.ValidatePropertyPath(selectorPart);
					return new PropertySelector(selectorPart);
				});

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

				pipedText = pipedText is null ? selectorPart : pipedText + SelectorDefaults.PropertyDelimiter + selectorPart;
			}

			return result;
		}
	}
}
