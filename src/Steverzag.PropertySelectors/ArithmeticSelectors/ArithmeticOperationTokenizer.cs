using Steverzag.PropertySelectors.Extensions;
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

				if (c == '"')
				{
					var start = _pos;
					_pos = _text.EndOfQuotedLiteralOrThrow(_pos) + 1;
					tokens.Add(new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos], start));
					continue;
				}

				if (c == SelectorDefaults.PropertyDelimiter[0] && tokens.Count > 0 && tokens[^1].Type == ArithmeticOperationTokenType.RParen)
				{
					tokens.Add(new ArithmeticOperationToken(ArithmeticOperationTokenType.Pipe, SelectorDefaults.PropertyDelimiter, _pos));
					_pos++;
					continue;
				}

				tokens.Add(c switch
				{
					'+' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Plus, "+", _pos),
					'-' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Minus, "-", _pos),
					'*' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Multiply, "*", _pos),
					'/' => new ArithmeticOperationToken(ArithmeticOperationTokenType.Divide, "/", _pos),
					'(' => new ArithmeticOperationToken(ArithmeticOperationTokenType.LParen, "(", _pos),
					')' => new ArithmeticOperationToken(ArithmeticOperationTokenType.RParen, ")", _pos),
					_ => throw new SelectorParseException($"Unexpected character '{c}'.", _text, _pos, c.ToString())
				});

				_pos++;
			}

			return tokens;
		}

		internal List<ArithmeticOperationToken> SanitizeNegations(List<ArithmeticOperationToken> tokens)
		{
			FoldNegativeNumbers(tokens);

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
				tokens.Insert(index + 1, new ArithmeticOperationToken(ArithmeticOperationTokenType.LParen, "(", tokens[index + 1].Position, Synthetic: true));
				tokens.Insert(index + 3, new ArithmeticOperationToken(ArithmeticOperationTokenType.RParen, ")", tokens[index + 2].Position + tokens[index + 2].Value.Length, Synthetic: true));
			}

			return tokens;
		}

		private static void FoldNegativeNumbers(List<ArithmeticOperationToken> tokens)
		{
			for (int i = tokens.Count - 2; i >= 0; i--)
			{
				var isUnaryPosition = i == 0 || tokens[i - 1].Type is not ArithmeticOperationTokenType.Selector and not ArithmeticOperationTokenType.RParen;
				var next = tokens[i + 1];

				if (tokens[i].Type == ArithmeticOperationTokenType.Minus && isUnaryPosition
					&& next.Type == ArithmeticOperationTokenType.Selector && next.Value.Length > 0 && char.IsDigit(next.Value[0]))
				{
					tokens[i] = new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, "-" + next.Value, tokens[i].Position);
					tokens.RemoveAt(i + 1);
				}
			}
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

				if (_pos < _text.Length && _text[_pos] is 'e' or 'E')
				{
					var exponentDigits = _pos + 1 < _text.Length && _text[_pos + 1] is '+' or '-' ? _pos + 2 : _pos + 1;
					if (exponentDigits < _text.Length && char.IsDigit(_text[exponentDigits]))
					{
						_pos = exponentDigits;
						while (_pos < _text.Length && char.IsDigit(_text[_pos]))
							_pos++;
					}
				}

				if (_pos < _text.Length && _text[_pos] is 'l' or 'L' or 'm' or 'M')
					_pos++;

				return new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos], start);
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

					if (c == '"')
					{
						_pos = _text.EndOfQuotedLiteralOrThrow(_pos);
					}
					else if (c == '(')
					{
						parenDepth++;
					}
					else if (c == ')')
					{
						if (parenDepth == 0)
							break;
						parenDepth--;
					}
					else if (parenDepth == 0 &&
							 (c == '+' || c == '-' || c == '*' || c == '/' || char.IsWhiteSpace(c)))
					{
						break;
					}

					_pos++;
				}

				return new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos], start);
			}

			var isPropertySelectorCandidate = char.IsLetter(_text[_pos]) || (_text[_pos] is '_' or '@');
			if (isPropertySelectorCandidate)
			{
				_pos++;

				var delimiter = SelectorDefaults.PropertyDelimiter[0];
				var inComplexSegment = false;
				var parenDepth = 0;

				while (_pos < _text.Length)
				{
					var c = _text[_pos];

					if (c == '"')
					{
						_pos = _text.EndOfQuotedLiteralOrThrow(_pos);
					}
					else if (parenDepth > 0)
					{
						if (c == '(') parenDepth++;
						else if (c == ')') parenDepth--;
					}
					else if (c == delimiter)
					{
						inComplexSegment = _pos + 1 < _text.Length && _text[_pos + 1] == complexSelectorStart;
					}
					else if (c == '(' && inComplexSegment)
					{
						parenDepth++;
					}
					else if ((c is '+' or '-' or '*' or '/' or '(' or ')') || char.IsWhiteSpace(c))
					{
						break;
					}

					_pos++;
				}

				return new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, _text[start.._pos], start);
			}

			throw new SelectorParseException($"Unexpected character '{_text[_pos]}'.", _text, _pos, _text[_pos].ToString());
		}
	}

}
