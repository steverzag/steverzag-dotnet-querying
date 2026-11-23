using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal class ArithmeticOperationTokenizer
	{
		private readonly string _text;
		private int _pos;

		public ArithmeticOperationTokenizer(string text)
		{
			_text = text;
		}

		public List<ArithmeticOperationToken> Tokenize()
		{
			var tokens = new List<ArithmeticOperationToken>();

			while (_pos < _text.Length)
			{
				var c = _text[_pos];

				if (char.IsWhiteSpace(c))
				{
					_pos++;
					continue;
				}

				if (c == '$' || char.IsDigit(c) || char.IsLetter(c) || c == '_' || c == '@')
				{
					tokens.Add(ReadSelector());
					continue;
				}

				tokens.Add(c switch
				{
					'+' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Plus, "+"),
					'-' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Minus, "-"),
					'*' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Multiply, "*"),
					'/' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Divide, "/"),
					'(' => new ArithmeticOperationToken(ArithmeticOperationTokenType.LParen, "("),
					')' => new ArithmeticOperationToken(ArithmeticOperationTokenType.RParen, ")"),
					_ => throw new Exception($"Invalid char: {c}")
				});

				_pos++;
			}

			return tokens;
		}

		internal List<ArithmeticOperationToken> SanitizeNegations(List<ArithmeticOperationToken> tokens)
		{
			var unaryMinusIndices = new List<int>();
			for (int i = 0; i < tokens.Count; i++)
			{
				if (tokens[i].Type == ArithmeticOperationTokenType.Minus)
				{
					bool isUnary = (i == 0 || tokens[i - 1].Type is not ArithmeticOperationTokenType.Selector and not ArithmeticOperationTokenType.LParen) &&
								   (i + 1 < tokens.Count && tokens[i + 1].Type == ArithmeticOperationTokenType.Selector);
					if (isUnary)
					{
						unaryMinusIndices.Add(i);
					}
				}
			}

			unaryMinusIndices.Reverse();

			foreach (var index in unaryMinusIndices)
			{
				tokens.Insert(index + 1, new ArithmeticOperationToken(ArithmeticOperationTokenType.LParen, "("));
				tokens.Insert(index + 3, new ArithmeticOperationToken(ArithmeticOperationTokenType.RParen, ")"));
			}

			return tokens;
		}

		private ArithmeticOperationToken ReadSelector()
		{
			var start = _pos;

			if (char.IsDigit(_text[_pos]))
			{
				while (_pos < _text.Length &&
					   (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
				{
					_pos++;
				}

				return new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos]);
			}

			var complexSelectorStart = SelectorDefaults.ComplexSelectorStart[0];
			var isComplexSelectorCandidate = _text[_pos] == complexSelectorStart;
			if (isComplexSelectorCandidate)
			{
				_pos++;

				int parenDepth = 0;

				while (_pos < _text.Length)
				{
					var c = _text[_pos];

					if (c == '(')
					{
						parenDepth++;
					}
					else if (c == ')')
					{
						parenDepth--;
					}
					else if (parenDepth == 0 &&
							 (c == '+' || c == '-' || c == '*' || c == '/' || char.IsWhiteSpace(c)))
					{
						break;
					}

					_pos++;
				}

				return new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos]);
			}

			var isPropertySelectorCandidate = char.IsLetter(_text[_pos]) || (_text[_pos] is '_' or '@');
			if (isPropertySelectorCandidate)
			{
				_pos++;

				while (_pos < _text.Length)
				{
					var c = _text[_pos];
					if ((c is '+' or '-' or '*' or '/' or '(' or ')') || char.IsWhiteSpace(c))
					{
						break;
					}

					_pos++;
				}

				return new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos]);
			}

			throw new Exception("Invalid selector");
		}
	}

}
