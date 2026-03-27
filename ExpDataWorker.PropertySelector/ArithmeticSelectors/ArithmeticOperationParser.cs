using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
{
	internal class ArithmeticOperationParser
	{
		private readonly List<ArithmeticOperationToken> _tokens;
		private int _pos;

		public ArithmeticOperationParser(List<ArithmeticOperationToken> tokens)
		{
			_tokens = tokens;
		}

		public PropertySelector Parse()
		{
			return ParseExpression();
		}

		private PropertySelector ParseExpression()
		{
			var left = ParseTerm();

			while (Match(ArithmeticOperationTokenType.Plus) || Match(ArithmeticOperationTokenType.Minus))
			{
				var op = Previous();
				var right = ParseTerm();

				left = new ArithmeticOperationSelector
				{
					LeftExpression = left,
					RightExpression = right,
					Operator = op.Type == ArithmeticOperationTokenType.Plus
						? ArithmeticOperator.Addition
						: ArithmeticOperator.Subtraction
				};
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

				left = new ArithmeticOperationSelector
				{
					LeftExpression = left,
					RightExpression = right,
					Operator = op.Type == ArithmeticOperationTokenType.Multiply
						? ArithmeticOperator.Multiplication
						: ArithmeticOperator.Division
				};
			}

			return left;
		}

		private PropertySelector ParseFactor()
		{
			if (Match(ArithmeticOperationTokenType.Minus))
			{
				var operand = ParseFactor();

				return new ArithmeticOperationSelector
				{
					LeftExpression = operand,
					Operator = ArithmeticOperator.Negation
				};
			}

			if (Match(ArithmeticOperationTokenType.Selector))
			{
				return ParseSelector(Previous().Value);
			}

			if (Match(ArithmeticOperationTokenType.LParen))
			{
				var expr = ParseExpression();
				Consume(ArithmeticOperationTokenType.RParen, "Missing ')'");
				return expr;
			}

			throw new Exception("Invalid expression");
		}

		private static PropertySelector ParseSelector(string text)
		{
			return text;
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
