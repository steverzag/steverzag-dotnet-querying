namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal class ArithmeticOperationParser
	{
		private readonly List<ArithmeticOperationToken> _tokens;
		private int _pos;
		private Func<string, PropertySelector> _createOperand = null!;
		private Func<PropertySelector, PropertySelector?, ArithmeticOperator, PropertySelector> _createOperation = null!;

		public ArithmeticOperationParser(List<ArithmeticOperationToken> tokens)
		{
			_tokens = tokens;
		}

		public PropertySelector<T> Parse<T>()
		{
			return (PropertySelector<T>)Parse(
				text => (PropertySelector<T>)text,
				(left, right, op) => new ArithmeticOperationSelector<T> { LeftExpression = left, RightExpression = right, Operator = op });
		}

		public PropertySelector Parse()
		{
			return Parse(
				text => (PropertySelector)text,
				(left, right, op) => new ArithmeticOperationSelector { LeftExpression = left, RightExpression = right, Operator = op });
		}

		private PropertySelector Parse(
			Func<string, PropertySelector> createOperand,
			Func<PropertySelector, PropertySelector?, ArithmeticOperator, PropertySelector> createOperation)
		{
			_pos = 0;
			_createOperand = createOperand;
			_createOperation = createOperation;
			return ParseExpression();
		}

		private PropertySelector ParseExpression()
		{
			var left = ParseTerm();

			while (Match(ArithmeticOperationTokenType.Plus) || Match(ArithmeticOperationTokenType.Minus))
			{
				var op = Previous();
				var right = ParseTerm();

				left = _createOperation(left, right, op.Type == ArithmeticOperationTokenType.Plus
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

				left = _createOperation(left, right, op.Type == ArithmeticOperationTokenType.Multiply
					? ArithmeticOperator.Multiplication
					: ArithmeticOperator.Division);
			}

			return left;
		}

		private PropertySelector ParseFactor()
		{
			if (Match(ArithmeticOperationTokenType.Minus))
			{
				var operand = ParseFactor();
				return _createOperation(operand, null, ArithmeticOperator.Negation);
			}

			if (Match(ArithmeticOperationTokenType.Selector))
			{
				return _createOperand(Previous().Value);
			}

			if (Match(ArithmeticOperationTokenType.LParen))
			{
				var expr = ParseExpression();
				Consume(ArithmeticOperationTokenType.RParen, "Missing ')'");
				return expr;
			}

			throw new Exception("Invalid expression");
		}

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

		private ArithmeticOperationToken Consume(ArithmeticOperationTokenType type, string message)
		{
			if (Check(type)) return _tokens[_pos++];

			throw new Exception(message);
		}
	}
}
