using Steverzag.PropertySelectors.Extensions;
using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.ValueSelectors;
using System.Linq.Expressions;
using System.Text;

namespace Steverzag.PropertySelectors
{
	/// <summary>
	/// A selector parsed against the root type <typeparamref name="T"/>: text such as <c>"Address.City"</c> or
	/// <c>"Phones.Id|$FN_AGGR_AVG"</c> that builds a LINQ expression over a <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// Because the root type is known, property types, function overloads and arithmetic are resolved when the text is
	/// parsed, so an error in the selector is reported then. Prefer this form whenever <typeparamref name="T"/> is known;
	/// the non-generic <see cref="PropertySelector"/> is for places where it isn't yet, such as request DTOs.
	/// The grammar is described in <c>docs/property-selectors.md</c>.
	/// </remarks>
	/// <typeparam name="T">The type the selector starts from.</typeparam>
	/// <example>
	/// <code>
	/// PropertySelector&lt;Person&gt; average = "Phones.Id|$FN_AGGR_AVG";
	/// var person = Expression.Parameter(typeof(Person), "p");
	/// Expression body = SelectorExpressions.CreateMemberAccess(person, average); // p.Phones.Select(x => x.Id).Average()
	/// </code>
	/// </example>
	public record PropertySelector<T> : PropertySelector
	{
		/// <summary>Initializes a selector with no text, for derived selectors such as functions.</summary>
		protected PropertySelector() : base()
		{
			SelectorType = InitSelectorTypeToApply();
		}

		internal PropertySelector(string plainValue) : base(plainValue)
		{
			if (SelectorType is not null)
				return;

			SelectorType = InitSelectorTypeToApply();
		}

		internal override Type? InitSelectorTypeToApply()
		{
			return typeof(T).GetPropertyType(this);
		}

		/// <summary>Parses selector text against <typeparamref name="T"/>.</summary>
		/// <param name="selector">The selector text, such as <c>"Address.City"</c> or <c>"Salary|$FN_MATH_ROUND(2)"</c>.</param>
		/// <exception cref="SelectorParseException">
		/// The text isn't a valid selector for <typeparamref name="T"/>: a syntax error, an unknown property or function, a
		/// wrong argument, a member hidden by the active <see cref="MemberFilter"/>, or a selector over the
		/// <see cref="SelectorLimits"/>.
		/// </exception>
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
					if (MemberFilter.IsActive)
						MemberFilter.PipeFrom(LastSegment(result));
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

		private static PropertySelector? LastSegment(PropertySelector? selector)
		{
			while (selector?.ChildSelector is not null)
				selector = selector.ChildSelector;
			return selector;
		}
	}



	/// <summary>
	/// The members every selector shares. Use <see cref="PropertySelector{T}"/> or <see cref="PropertySelector"/>; this
	/// type can't be derived from outside the library.
	/// </summary>
	public abstract record SelectorBase
	{
		/// <summary>
		/// The text of this part of the chain alone: a property path such as <c>Address.City</c>, a function token, or a
		/// literal. Use <see cref="PropertySelector.ToString"/> for the whole selector.
		/// </summary>
		public string PlainValue { get; internal init; } = string.Empty;

		/// <summary>
		/// The type this part declares, or <see langword="null"/> when it isn't known yet: in the non-generic form, or for a
		/// function whose result depends on an argument that isn't typed.
		/// </summary>
		public virtual Type? SelectorType { get; internal init; }

		/// <summary>
		/// The type of the whole chain from this part on, which is the type its last part declares, or
		/// <see langword="null"/> when that isn't known.
		/// </summary>
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
		/// <summary>The part that follows this one after a <c>|</c>, or <see langword="null"/> if this is the last part.</summary>
		public PropertySelector? ChildSelector { get; internal set; }

		/// <summary>
		/// Builds the expression for this part alone, given its target. To build the whole chain, use
		/// <see cref="SelectorExpressions.CreateMemberAccess"/>, which also applies every <see cref="ChildSelector"/>.
		/// </summary>
		public Func<Expression?, Expression> PropertyAccess { get; internal init; } = null!; // assigned by every PropertySelector constructor
		internal abstract Type? InitSelectorTypeToApply();
	}

	/// <summary>
	/// A selector parsed without a root type, for request DTOs and serialization where the type isn't known yet.
	/// </summary>
	/// <remarks>
	/// Property paths are resolved when the expression is built, so an unknown property is reported then, not when the
	/// text is parsed; the error is the same <see cref="SelectorParseException"/> the generic form reports. Convert to the
	/// generic form with <see cref="Extensions.PropertySelectorExtensions.ToGeneric{T}"/> once the type is known.
	/// </remarks>
	public record PropertySelector : SelectorBase
	{
		internal override Type? InitSelectorTypeToApply()
		{
			return SelectorType;
		}

		/// <summary>Initializes a selector with no text, for derived selectors such as functions.</summary>
		protected PropertySelector()
		{
			PlainValue = string.Empty;
			PropertyAccess = (target) => target ?? Expression.Constant(null);
		}

		internal PropertySelector(string plainValue)
		{
			PlainValue = plainValue;
			PropertyAccess = (target) =>
			{
				var propertyNames = PlainValue.Split('.');

				return SelectorExpressions.CreateMemberAccessRec(target ?? throw new ArgumentNullException(nameof(target), $"Property path '{PlainValue}' needs a target expression."), propertyNames, 0);
			};
		}

		/// <summary>
		/// Returns selector text that parses back to an equivalent selector, in either form. The text is normalized, not
		/// echoed: for example, arithmetic keeps only the parentheses its structure needs, and literals are written as
		/// <see cref="SelectorLiteral.Format(object)"/> writes them.
		/// </summary>
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

		private protected virtual string SegmentToString() => PlainValue;

		/// <summary>Parses selector text without a root type.</summary>
		/// <param name="selector">The selector text, such as <c>"Address.City"</c> or <c>"Salary|$FN_MATH_ROUND(2)"</c>.</param>
		/// <exception cref="SelectorParseException">
		/// The text isn't a valid selector: a syntax error, an unknown function, a wrong argument, or a selector over the
		/// <see cref="SelectorLimits"/>. An unknown property is reported later, when the expression is built.
		/// </exception>
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
