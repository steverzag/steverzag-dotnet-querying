using System.Collections.Concurrent;
using System.Reflection;
using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.MetaData
{
	/// <summary>A registered function as <see cref="FunctionCatalog"/> describes it, for UIs and documentation.</summary>
	/// <param name="Token">The function's token, such as <c>$FN_MATH_ROUND</c>.</param>
	/// <param name="Category">The group the function belongs to, for UIs.</param>
	/// <param name="Piped">What a value piped into the function with <c>|</c> means for it.</param>
	/// <param name="PipedHint">An extra sentence for the error when the function rejects a piped value, if any.</param>
	/// <param name="Arguments">What the function's arguments are typed against in the generic form.</param>
	/// <param name="HasGenericForm">Whether the function can be used in <see cref="PropertySelector{T}"/>.</param>
	/// <param name="HasNonGenericForm">Whether the function can be used in <see cref="PropertySelector"/>.</param>
	/// <param name="Signatures">The function's overloads.</param>
	public sealed record FunctionDescriptor(
		string Token,
		FunctionCategory Category,
		PipedForm Piped,
		string? PipedHint,
		ArgumentScope Arguments,
		bool HasGenericForm,
		bool HasNonGenericForm,
		IReadOnlyList<FunctionSignature> Signatures);

	/// <summary>One overload of a function that accepts a piped value of a given type, as <see cref="FunctionCatalog.After"/> returns it.</summary>
	/// <param name="Function">The function.</param>
	/// <param name="Signature">The overload that accepts the piped value.</param>
	/// <param name="Parameters">The arguments still to write, without the one the piped value fills.</param>
	/// <param name="Rest">The constraint for any number of extra arguments, if the overload takes them.</param>
	/// <param name="ResultType">
	/// The type the function returns for this piped value, or <see langword="null"/> when it depends on an argument.
	/// </param>
	public sealed record FunctionMatch(
		FunctionDescriptor Function,
		FunctionSignature Signature,
		IReadOnlyList<ArgumentConstraint> Parameters,
		ArgumentConstraint? Rest,
		Type? ResultType)
	{
		/// <summary>A C#-style name for <see cref="ResultType"/>, such as <c>decimal?</c>, or <see langword="null"/> when it isn't known.</summary>
		public string? ResultTypeName => ResultType is null ? null : TypeDisplay.Name(ResultType);
	}

	/// <summary>
	/// Lists the registered functions, including those registered from other assemblies, so a UI can offer them.
	/// </summary>
	/// <remarks>
	/// Combine it with <see cref="PropertiesExplorer{T}"/>: pick a property from the tree, ask <see cref="After"/> what can
	/// follow its type, and repeat with the result type.
	/// </remarks>
	public static class FunctionCatalog
	{
		private static readonly ConcurrentDictionary<(string Token, SelectorRegistration Registration), FunctionDescriptor> Descriptors = new();

		/// <summary>Every registered function, ordered by token.</summary>
		public static IReadOnlyList<FunctionDescriptor> All =>
			[.. SelectorRegistry.Snapshot().Select(entry => Describe(entry.Key, entry.Value))];

		/// <summary>Finds a registered function by its token.</summary>
		/// <param name="token">The token, such as <c>$FN_MATH_ROUND</c>. Tokens are case-sensitive.</param>
		/// <returns>The function, or <see langword="null"/> if no function has that token.</returns>
		public static FunctionDescriptor? Find(string token) =>
			SelectorRegistry.TryGetRegistration(token, out var registration) ? Describe(token, registration) : null;

		/// <summary>Answers "what can follow a value of this type with <c>|</c>?".</summary>
		/// <param name="pipedType">The type of the value before the <c>|</c>.</param>
		/// <returns>
		/// One match per overload that accepts the type. Functions that reject a piped value are never listed. With valid
		/// arguments, each match parses and declares the result type it reports.
		/// </returns>
		/// <example>
		/// <code>
		/// FunctionCatalog.After(typeof(decimal));   // MATH_ROUND() → decimal, MATH_ROUND(an integer) → decimal, …
		/// FunctionCatalog.After(typeof(List&lt;int&gt;)); // AGGR_SUM() → int, AGGR_AVG() → double?, ELM_FIRST() → int, …
		/// </code>
		/// </example>
		public static IReadOnlyList<FunctionMatch> After(Type pipedType)
		{
			ArgumentNullException.ThrowIfNull(pipedType);

			var matches = new List<FunctionMatch>();
			foreach (var function in All)
			{
				foreach (var signature in function.Signatures)
				{
					if (Match(function, signature, pipedType) is { } match)
						matches.Add(match);
				}
			}

			return matches;
		}

		private static FunctionMatch? Match(FunctionDescriptor function, FunctionSignature signature, Type pipedType)
		{
			if (function.Piped == PipedForm.Rejected || !signature.Target.Accepts(pipedType))
				return null;

			IReadOnlyList<ArgumentConstraint> parameters = signature.Parameters;
			var argumentTypes = new List<Type?>();

			if (function.Piped == PipedForm.FirstArgument)
			{
				if ((signature.Parameters.Count == 0 && signature.Rest is null)
					|| !signature.ConstraintAt(0).Accepts(pipedType))
					return null;

				parameters = [.. signature.Parameters.Skip(1)];
				argumentTypes.Add(pipedType);
			}

			argumentTypes.AddRange(parameters.Select(_ => (Type?)null));

			Type? resultType;
			try
			{
				resultType = signature.ResultType(pipedType, argumentTypes);
			}
			catch (Exception e) when (e is ArgumentException or InvalidOperationException or NotSupportedException)
			{
				return null;
			}

			return new FunctionMatch(function, signature, parameters, signature.Rest, resultType);
		}

		private static FunctionDescriptor Describe(string token, SelectorRegistration registration) =>
			Descriptors.GetOrAdd((token, registration), static key =>
			{
				var (token, registration) = key;
				var selectorType = registration.NonGenericSelectorType
					?? registration.OpenGenericSelectorType!.MakeGenericType(typeof(object));
				var attribute = (registration.NonGenericSelectorType ?? registration.OpenGenericSelectorType)!
					.GetCustomAttribute<SelectorTokenAttribute>();

				return new FunctionDescriptor(
					token,
					attribute?.Category ?? FunctionCategory.Other,
					attribute?.Piped ?? PipedForm.Target,
					attribute?.PipedHint,
					attribute?.Arguments ?? ArgumentScope.Root,
					registration.OpenGenericSelectorType is not null,
					registration.NonGenericSelectorType is not null,
					FunctionSignatures.Of(selectorType));
			});
	}
}
