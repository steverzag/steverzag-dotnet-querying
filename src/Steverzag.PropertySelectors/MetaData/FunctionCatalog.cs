using System.Collections.Concurrent;
using System.Reflection;
using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.MetaData
{
	public sealed record FunctionDescriptor(
		string Token,
		FunctionCategory Category,
		PipedForm Piped,
		string? PipedHint,
		ArgumentScope Arguments,
		bool HasGenericForm,
		bool HasNonGenericForm,
		IReadOnlyList<FunctionSignature> Signatures);

	public sealed record FunctionMatch(
		FunctionDescriptor Function,
		FunctionSignature Signature,
		IReadOnlyList<ArgumentConstraint> Parameters,
		ArgumentConstraint? Rest,
		Type? ResultType)
	{
		public string? ResultTypeName => ResultType is null ? null : TypeDisplay.Name(ResultType);
	}

	public static class FunctionCatalog
	{
		private static readonly ConcurrentDictionary<(string Token, SelectorRegistration Registration), FunctionDescriptor> Descriptors = new();

		public static IReadOnlyList<FunctionDescriptor> All =>
			[.. SelectorRegistry.Snapshot().Select(entry => Describe(entry.Key, entry.Value))];

		public static FunctionDescriptor? Find(string token) =>
			SelectorRegistry.TryGetRegistration(token, out var registration) ? Describe(token, registration) : null;

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
