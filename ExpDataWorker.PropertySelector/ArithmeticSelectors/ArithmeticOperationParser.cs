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

		//TODO: Non generic implementaion 
		public PropertySelector<T> Parse<T>()
		{
			return ParseExpression<T>();
		}

		private PropertySelector<T> ParseExpression<T>()
		{
			var left = ParseTerm<T>();

			while (Match(ArithmeticOperationTokenType.Plus) || Match(ArithmeticOperationTokenType.Minus))
			{
				var op = Previous();
				var right = ParseTerm<T>();

				left = new ArithmeticOperationSelector<T>
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

		private PropertySelector<T> ParseTerm<T>()
		{
			var left = ParseFactor<T>();

			while (Match(ArithmeticOperationTokenType.Multiply) || Match(ArithmeticOperationTokenType.Divide))
			{
				var op = Previous();
				var right = ParseFactor<T>();

				left = new ArithmeticOperationSelector<T>
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

		private PropertySelector<T> ParseFactor<T>()
		{
			if (Match(ArithmeticOperationTokenType.Minus))
			{
				var operand = ParseFactor<T>();

				return new ArithmeticOperationSelector<T>
				{
					LeftExpression = operand,
					Operator = ArithmeticOperator.Negation
				};
			}

			if (Match(ArithmeticOperationTokenType.Selector))
			{
				return ParseSelector<T>(Previous().Value);
			}

			if (Match(ArithmeticOperationTokenType.LParen))
			{
				var expr = ParseExpression<T>();
				Consume(ArithmeticOperationTokenType.RParen, "Missing ')'");
				return expr;
			}

			throw new Exception("Invalid expression");
		}

		private static PropertySelector<T> ParseSelector<T>(string text)
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
