using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors
{
	internal sealed record FunctionCall(FunctionSignature? Signature, Type? DeclaredType, Func<Expression?, Expression> PropertyAccess)
	{
		private static readonly ConcurrentDictionary<Type, ArgumentScope> Scopes = new();

		internal static FunctionCall Resolve(string token, IReadOnlyList<FunctionSignature> signatures,
			IReadOnlyList<PropertySelector> arguments, Type? targetType, Type selectorType)
		{
			var argumentTypes = arguments.Select(a => a.Type).ToList();
			var signature = SignatureResolver.Resolve(token, signatures, arguments, targetType, argumentTypes);

			if (signature is not null && SignatureResolver.IsFullyKnown(signature, arguments, targetType, argumentTypes))
				return new(signature, signature.ResultType(targetType, argumentTypes), signature.Bind(arguments));

			var scope = Scopes.GetOrAdd(selectorType, static type => type.GetCustomAttribute<SelectorTokenAttribute>()?.Arguments ?? ArgumentScope.Root);
			var parsedBinding = signature?.Bind(arguments);

			return new(signature, signature?.ResultType(targetType, argumentTypes), target =>
			{
				var builtTypes = arguments.Select(a => BuiltType(target, a, scope)).ToList();
				FunctionSignature resolved;
				try
				{
					resolved = SignatureResolver.Resolve(token, signatures, arguments, target?.Type, builtTypes)
						?? throw new InvalidOperationException($"{token} could not be resolved from the built argument types.");
					resolved.ResultType(target?.Type, builtTypes);
				}
				catch (Exception e) when (e is InvalidOperationException or ArgumentException && e is not SelectorParseException)
				{
					throw SignatureResolver.BuildError(token, arguments, e);
				}
				var binding = parsedBinding is not null && ReferenceEquals(resolved, signature) ? parsedBinding : resolved.Bind(arguments);
				return binding(target);
			});
		}

		private static Type? BuiltType(Expression? target, PropertySelector argument, ArgumentScope scope)
		{
			if (scope == ArgumentScope.TargetElement)
			{
				return target is not null && SelectorExpressions.TryGetEnumerableElementType(target.Type, out var element) && element is not null
					? SelectorExpressions.CreateMemberAccess(Expression.Parameter(element, "x"), argument).Type
					: null;
			}

			return SelectorExpressions.ResolveArgument(target, argument).Type;
		}
	}

	internal static class SignatureResolver
	{
		internal static FunctionSignature? Resolve(string token, IReadOnlyList<FunctionSignature> signatures,
			IReadOnlyList<PropertySelector> arguments, Type? targetType, IReadOnlyList<Type?> argumentTypes)
		{
			var byCount = signatures.Where(s => s.Accepts(arguments.Count)).ToList();
			if (byCount.Count == 0)
				throw new ArgumentException(FunctionSelectorFormatter.ArgumentCountMessage(token, arguments, signatures));

			var candidates = new List<(FunctionSignature Signature, ConstraintMatch[] Matches)>();
			foreach (var signature in byCount)
			{
				var matches = Match(signature, arguments, targetType, argumentTypes);
				if (!matches.Contains(ConstraintMatch.None))
					candidates.Add((signature, matches));
			}

			if (candidates.Count == 0)
				throw new InvalidOperationException(MismatchMessage(token, byCount, arguments, targetType, argumentTypes));

			if (candidates.Count == 1)
				return candidates[0].Signature;

			if (DependsOnAnUnknownType(candidates, arguments.Count))
				return null;

			var best = candidates.Where(c => candidates.All(other => ReferenceEquals(c.Signature, other.Signature) || IsBetter(c.Matches, other.Matches))).ToList();
			if (best.Count == 1)
				return best[0].Signature;

			throw new InvalidOperationException(
				$"{Written(token, arguments)} is ambiguous between {string.Join(" and ", candidates.Select(c => c.Signature))}.");
		}

		internal static bool IsFullyKnown(FunctionSignature signature, IReadOnlyList<PropertySelector> arguments, Type? targetType, IReadOnlyList<Type?> argumentTypes)
			=> !Match(signature, arguments, targetType, argumentTypes).Contains(ConstraintMatch.Unknown);

		internal static SelectorParseException BuildError(string token, IReadOnlyList<PropertySelector> arguments, Exception error)
		{
			var function = FunctionSelectorFormatter.Format(token, arguments);
			var position = arguments.Count > 0 && arguments[0] is PipedValueSelector piped ? piped.PipedText.Length + 1 : 0;

			return new SelectorParseException(error.Message, Written(token, arguments), position, function, error);
		}

		private static ConstraintMatch[] Match(FunctionSignature signature, IReadOnlyList<PropertySelector> arguments, Type? targetType, IReadOnlyList<Type?> argumentTypes)
		{
			var matches = new ConstraintMatch[arguments.Count + 1];
			matches[0] = signature.Target.Match(null, targetType);
			for (var i = 0; i < arguments.Count; i++)
				matches[i + 1] = signature.ConstraintAt(i).Match(arguments[i], argumentTypes[i]);
			return matches;
		}

		private static bool DependsOnAnUnknownType(List<(FunctionSignature Signature, ConstraintMatch[] Matches)> candidates, int argumentCount)
		{
			for (var position = 0; position <= argumentCount; position++)
			{
				if (!candidates.Any(c => c.Matches[position] == ConstraintMatch.Unknown))
					continue;

				var constraints = candidates.Select(c => position == 0 ? c.Signature.Target : c.Signature.ConstraintAt(position - 1)).Distinct().Count();
				if (constraints > 1)
					return true;
			}

			return false;
		}

		private static bool IsBetter(ConstraintMatch[] candidate, ConstraintMatch[] other)
		{
			var strictlyBetter = false;
			for (var i = 0; i < candidate.Length; i++)
			{
				if (candidate[i] > other[i])
					return false;
				if (candidate[i] < other[i])
					strictlyBetter = true;
			}
			return strictlyBetter;
		}

		private static string MismatchMessage(string token, List<FunctionSignature> byCount, IReadOnlyList<PropertySelector> arguments,
			Type? targetType, IReadOnlyList<Type?> argumentTypes)
		{
			var written = Written(token, arguments);

			if (byCount.Count > 1)
			{
				var types = arguments.Select((_, i) => argumentTypes[i] is { } t ? TypeNames.Describe(t) : "?");
				return $"{token} has no overload for ({string.Join(", ", types)}): '{written}'. It takes {string.Join(" or ", byCount)}.";
			}

			var signature = byCount[0];
			if (signature.Target.Match(null, targetType) == ConstraintMatch.None)
				return $"{token} must be applied to {signature.Target.Description}, not {TypeNames.WithArticle(targetType!)}: '{written}'.";

			var offset = arguments.Count > 0 && arguments[0] is PipedValueSelector ? 1 : 0;
			for (var i = 0; i < arguments.Count; i++)
			{
				var constraint = signature.ConstraintAt(i);
				if (constraint.Match(arguments[i], argumentTypes[i]) != ConstraintMatch.None)
					continue;

				var subject = arguments[i] is PipedValueSelector piped
					? $"the piped value '{piped.PipedText}'"
					: $"argument {i + 1 - offset} ('{arguments[i]}')";
				var actual = constraint.IsLiteral || argumentTypes[i] is null ? "" : $", not {TypeNames.WithArticle(argumentTypes[i]!)}";
				return $"{token}: {subject} must be {constraint.Description}{actual}: '{written}'.";
			}

			return $"{token} can't be called with these arguments: '{written}'.";
		}

		private static string Written(string token, IReadOnlyList<PropertySelector> arguments)
		{
			var text = FunctionSelectorFormatter.Format(token, arguments);
			return arguments.Count > 0 && arguments[0] is PipedValueSelector piped
				? $"{piped.PipedText}{SelectorDefaults.PropertyDelimiter}{text}"
				: text;
		}
	}
}
