using Steverzag.PropertySelectors.Extensions;
using Steverzag.PropertySelectors.ValueSelectors;
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
					tokens.Add(ReadChainTail(start));
					continue;
				}

				if (c == SelectorDefaults.ReservedCharacter)
					throw PropertySelectorExtensionsInternal.ReservedCharacter(_text, _pos);

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
					&& next.Type == ArithmeticOperationTokenType.Selector && ValueSelectorFactory.IsNumberLiteral(next.Value))
				{
					tokens[i] = new ArithmeticOperationToken(ArithmeticOperationTokenType.Selector, "-" + next.Value, tokens[i].Position);
					tokens.RemoveAt(i + 1);
				}
			}
		}

		private ArithmeticOperationToken ReadSelector()
		{
			var start = _pos;
			var c = _text[_pos];

			if (char.IsDigit(c))
			{
				ReadNumber();
				return ReadChainTail(start);
			}

			if (c == SelectorDefaults.ComplexSelectorStart[0] || char.IsLetter(c) || c is '_' or '@')
				return ReadChain(start, isFunction: c == SelectorDefaults.ComplexSelectorStart[0]);

			throw new SelectorParseException($"Unexpected character '{c}'.", _text, _pos, c.ToString());
		}

		private void ReadNumber()
		{
			while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
				_pos++;

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
		}

		private ArithmeticOperationToken ReadChainTail(int start)
			=> PipeFollows() ? ReadChain(start, isFunction: false) : Token(start);

		private ArithmeticOperationToken ReadChain(int start, bool isFunction)
		{
			var delimiter = SelectorDefaults.PropertyDelimiter[0];
			var parenDepth = 0;

			while (_pos < _text.Length)
			{
				var c = _text[_pos];

				if (c == '"')
				{
					_pos = _text.EndOfQuotedLiteralOrThrow(_pos) + 1;
					continue;
				}

				if (parenDepth > 0)
				{
					if (c == '(') parenDepth++;
					else if (c == ')') parenDepth--;
				}
				else if (c == delimiter)
				{
					_pos++;
					SkipWhitespace();
					isFunction = _pos < _text.Length && _text[_pos] == SelectorDefaults.ComplexSelectorStart[0];
					continue;
				}
				else if (char.IsWhiteSpace(c))
				{
					if (!PipeFollows())
						break;
					continue;
				}
				else if (c == '(' && isFunction)
				{
					parenDepth++;
				}
				else if (c is '+' or '-' or '*' or '/' or '(' or ')')
				{
					break;
				}

				_pos++;
			}

			return Token(start);
		}

		private bool PipeFollows()
		{
			var next = _pos;
			while (next < _text.Length && char.IsWhiteSpace(_text[next]))
				next++;

			if (next >= _text.Length || _text[next] != SelectorDefaults.PropertyDelimiter[0])
				return false;

			_pos = next;
			return true;
		}

		private void SkipWhitespace()
		{
			while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
				_pos++;
		}

		private ArithmeticOperationToken Token(int start)
			=> new(ArithmeticOperationTokenType.Selector, _text[start.._pos].TrimEnd(), start);
	}

}
