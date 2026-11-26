using Steverzag.PropertySelectors.ArithmeticSelectors;
using System.Runtime.CompilerServices;

namespace Steverzag.PropertySelectors
{
	public static class SelectorLimits
	{
		public const int DefaultMaxLength = 2000;
		public const int DefaultMaxDepth = 32;
		public const int MaxDepthCeiling = 256;

		private static int _maxLength = DefaultMaxLength;
		private static int _maxDepth = DefaultMaxDepth;

		[ThreadStatic]
		private static int _nesting;

		[ThreadStatic]
		private static int _parses;

		[ThreadStatic]
		private static string? _outermost;

		public static int MaxLength
		{
			get => Volatile.Read(ref _maxLength);
			set
			{
				ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
				Volatile.Write(ref _maxLength, value);
			}
		}

		public static int MaxDepth
		{
			get => Volatile.Read(ref _maxDepth);
			set
			{
				ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
				ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDepthCeiling);
				Volatile.Write(ref _maxDepth, value);
			}
		}

		internal static TSelector Parse<TSelector>(string selector, Func<string, TSelector> parse) where TSelector : PropertySelector
		{
			if (selector is not null && selector.Length > MaxLength)
				throw new SelectorParseException(
					$"The selector is {selector.Length} characters long; the limit is {MaxLength} ({nameof(SelectorLimits)}.{nameof(MaxLength)}).",
					selector) { IsLimit = true };

			var maxDepth = MaxDepth;
			if (_parses++ == 0)
				_outermost = selector;
			try
			{
				Enter(maxDepth);
				try
				{
					var result = parse(selector!);

					if (_parses == 1 && DepthWithin(result, maxDepth) > maxDepth)
						throw TooDeep(maxDepth);

					return result;
				}
				finally
				{
					_nesting--;
				}
			}
			finally
			{
				if (--_parses == 0)
					_outermost = null;
			}
		}

		internal static void Enter(int maxDepth)
		{
			if (_nesting >= maxDepth)
				throw TooDeep(maxDepth);

			if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
				throw new SelectorParseException("The selector is nested too deeply to parse.", _outermost ?? string.Empty) { IsLimit = true };

			_nesting++;
		}

		internal static void Exit() => _nesting--;

		internal static void Resume() => _nesting++;

		private static SelectorParseException TooDeep(int maxDepth)
			=> new($"The selector is nested more than {maxDepth} levels deep ({nameof(SelectorLimits)}.{nameof(MaxDepth)}).", _outermost ?? string.Empty) { IsLimit = true };

		internal static int Depth(PropertySelector? selector) => DepthWithin(selector, int.MaxValue - 1);

		private static int DepthWithin(PropertySelector? selector, int budget)
		{
			var total = 0;
			for (var current = selector; current is not null; current = current.ChildSelector)
			{
				var cost = Cost(current);
				if (cost > budget)
					return budget + 1;

				var inner = total;
				switch (current)
				{
					case IFunctionSelector function:
						var parameters = function.Parameters;
						for (var i = 0; i < parameters.Count; i++)
						{
							inner = Math.Max(inner, DepthWithin(parameters[i], budget - cost));
							if (cost + inner > budget)
								return budget + 1;
						}
						break;

					case IArithmeticOperationSelector operation:
						inner = Math.Max(inner, DepthWithin(operation.LeftExpression, budget - cost));
						if (operation.RightExpression is { } right && cost + inner <= budget)
							inner = Math.Max(inner, DepthWithin(right, budget - cost));
						if (cost + inner > budget)
							return budget + 1;
						break;
				}

				total = cost + inner;
			}

			return total;
		}

		private static int Cost(PropertySelector selector) => selector switch
		{
			PipedValueSelector => 0,
			IFunctionSelector or IArithmeticOperationSelector => 1,
			_ when IsPropertyPath(selector) => selector.PlainValue.AsSpan().Count('.') + 1,
			_ => 1,
		};

		private static bool IsPropertyPath(PropertySelector selector)
		{
			var type = selector.GetType();
			return type == typeof(PropertySelector)
				|| (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PropertySelector<>));
		}
	}
}
