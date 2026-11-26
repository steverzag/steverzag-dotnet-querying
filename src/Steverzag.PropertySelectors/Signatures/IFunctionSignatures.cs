using System.Collections.Concurrent;
using System.Reflection;

namespace Steverzag.PropertySelectors
{
	public interface IFunctionSignatures
	{
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
