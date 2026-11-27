using System.Collections.Concurrent;
using System.Reflection;

namespace Steverzag.PropertySelectors
{
	/// <summary>
	/// Declares a function's overloads, so they can be read without creating the function. Every function selector must
	/// implement it; the registry rejects one that doesn't.
	/// </summary>
	public interface IFunctionSignatures
	{
		/// <summary>The function's overloads. Return the same table from the generic and the non-generic form.</summary>
		static abstract IReadOnlyList<FunctionSignature> Signatures { get; }
	}

	internal static class FunctionSignatures
	{
		private static readonly ConcurrentDictionary<Type, IReadOnlyList<FunctionSignature>> Tables = new();

		private static readonly MethodInfo ReadMethod =
			typeof(FunctionSignatures).GetMethod(nameof(Read), BindingFlags.NonPublic | BindingFlags.Static)!;

		internal static bool IsDeclaredBy(Type selectorType) =>
			selectorType.GetInterfaces().Contains(typeof(IFunctionSignatures));

		internal static IReadOnlyList<FunctionSignature> Of(Type selectorType) =>
			Tables.GetOrAdd(selectorType, static type => IsDeclaredBy(type)
				? (IReadOnlyList<FunctionSignature>)ReadMethod.MakeGenericMethod(type).Invoke(null, null)!
				: throw new InvalidOperationException(
					$"{type.FullName} declares no signatures. A function selector must implement {nameof(IFunctionSignatures)}."));

		private static IReadOnlyList<FunctionSignature> Read<TSelector>() where TSelector : IFunctionSignatures =>
			TSelector.Signatures;
	}
}
