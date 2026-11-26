using Steverzag.PropertySelectors.Extensions;

namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal class ArithmeticOperationParser
	{
		private readonly string _text;
		private readonly List<ArithmeticOperationToken> _tokens;
		private int _pos;
		private Func<string, PropertySelector> _createOperand = null!;
		private Func<PropertySelector, PropertySelector?, ArithmeticOperator, PropertySelector> _createOperation = null!;
		private Func<PropertySelector, string, ArithmeticOperationToken, PropertySelector> _createChain = null!;

		public ArithmeticOperationParser(string text, List<ArithmeticOperationToken> tokens)
		{
			_text = text;
			_tokens = tokens;
		}

		public PropertySelector<T> Parse<T>()
		{
			return (PropertySelector<T>)Parse(
				text => (PropertySelector<T>)text,
				(left, right, op) => new ArithmeticOperationSelector<T> { LeftExpression = left, RightExpression = right, Operator = op },
				(head, groupText, tail) => PropertySelector<T>.Chain(_text, TailParts(tail), (PropertySelector<T>)head, groupText));
		}

		public PropertySelector Parse()
		{
			return Parse(
				text => (PropertySelector)text,
				(left, right, op) => new ArithmeticOperationSelector { LeftExpression = left, RightExpression = right, Operator = op },
				(head, groupText, tail) => PropertySelector.Chain(_text, TailParts(tail), head, groupText));
		}

		private PropertySelector Parse(
			Func<string, PropertySelector> createOperand,
			Func<PropertySelector, PropertySelector?, ArithmeticOperator, PropertySelector> createOperation,
			Func<PropertySelector, string, ArithmeticOperationToken, PropertySelector> createChain)
		{
			_pos = 0;
			_createOperand = createOperand;
			_createOperation = createOperation;
			_createChain = createChain;

			var expression = ParseExpression();
			if (_pos < _tokens.Count)
			{
				var unexpected = _tokens[_pos];
				throw new SelectorParseException($"Unexpected '{unexpected.Value}': an operator is missing before it.", _text, unexpected.Position, unexpected.Value);
			}

			return expression;
		}

		private PropertySelector ParseExpression()
		{
			var left = ParseTerm();

			while (Match(ArithmeticOperationTokenType.Plus) || Match(ArithmeticOperationTokenType.Minus))
			{
				var op = Previous();
				var right = ParseTerm();

				left = CreateOperation(op, left, right, op.Type == ArithmeticOperationTokenType.Plus
					? ArithmeticOperator.Addition
					: ArithmeticOperator.Subtraction);
			}

			return left;
		}

		private PropertySelector ParseTerm()
		{
			var left = ParseFactor();

			while (Match(ArithmeticOperationTokenType.Multiply) || Match(ArithmeticOperationTokenType.Divide))
			{
				var op = Previous();
				var right = ParseFactor();

				left = CreateOperation(op, left, right, op.Type == ArithmeticOperationTokenType.Multiply
					? ArithmeticOperator.Multiplication
					: ArithmeticOperator.Division);
			}

			return left;
		}

		private PropertySelector ParseFactor()
		{
			if (Match(ArithmeticOperationTokenType.Minus))
			{
				var op = Previous();
				var operand = ParseNestedFactor();
				return CreateOperation(op, operand, null, ArithmeticOperator.Negation);
			}

			if (Match(ArithmeticOperationTokenType.Selector))
			{
				var operand = Previous();
				try
				{
					return ParseOperand(operand.Value);
				}
				catch (Exception e) when (SelectorParseException.IsSelectorError(e))
				{
					throw SelectorParseException.From(e, _text, operand.Position, operand.Value);
				}
			}

			if (Match(ArithmeticOperationTokenType.LParen))
			{
				var open = Previous();
				var expr = open.Synthetic ? ParseExpression() : ParseNestedExpression();
				if (!Match(ArithmeticOperationTokenType.RParen))
					throw new SelectorParseException("Unbalanced parentheses: 1 unclosed '('.", _text, open.Position, "(");

				var close = Previous();
				if (Match(ArithmeticOperationTokenType.Pipe))
				{
					var pipe = Previous();
					if (!Match(ArithmeticOperationTokenType.Selector))
						throw new SelectorParseException($"Empty part: '{SelectorDefaults.PropertyDelimiter}' must sit between two parts.", _text, pipe.Position, pipe.Value);

					expr = _createChain(expr, _text[open.Position..(close.Position + 1)], Previous());
				}

				return expr;
			}

			if (_pos < _tokens.Count)
			{
				var unexpected = _tokens[_pos];
				throw new SelectorParseException($"Unexpected '{unexpected.Value}': an operand is expected here.", _text, unexpected.Position, unexpected.Value);
			}

			throw new SelectorParseException("The expression ends where an operand is expected.", _text, _text.Length, null);
		}

		private PropertySelector CreateOperation(ArithmeticOperationToken op, PropertySelector left, PropertySelector? right, ArithmeticOperator @operator)
		{
			try
			{
				return _createOperation(left, right, @operator);
			}
			catch (Exception e) when (SelectorParseException.IsSelectorError(e))
			{
				throw SelectorParseException.From(e, _text, op.Position, op.Value);
			}
		}

		private PropertySelector ParseNestedFactor()
		{
			SelectorLimits.Enter(SelectorLimits.MaxDepth);
			try
			{
				return ParseFactor();
			}
			finally
			{
				SelectorLimits.Exit();
			}
		}

		private PropertySelector ParseNestedExpression()
		{
			SelectorLimits.Enter(SelectorLimits.MaxDepth);
			try
			{
				return ParseExpression();
			}
			finally
			{
				SelectorLimits.Exit();
			}
		}

		private PropertySelector ParseOperand(string text)
		{
			SelectorLimits.Exit();
			try
			{
				return _createOperand(text);
			}
			finally
			{
				SelectorLimits.Resume();
			}
		}

		private static IEnumerable<SelectorPartText> TailParts(ArithmeticOperationToken tail)
			=> tail.Value.AnalyzePropertySelector().Parts.Select(part => part with { Offset = part.Offset + tail.Position });

		private bool Match(ArithmeticOperationTokenType type)
		{
			if (Check(type))
			{
				_pos++;
				return true;
			}
			return false;
		}

		private bool Check(ArithmeticOperationTokenType type)
		{
			if (_pos >= _tokens.Count) return false;
			return _tokens[_pos].Type == type;
		}

		private ArithmeticOperationToken Previous() => _tokens[_pos - 1];
	}
}
