using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ExpDataWorker.PropertySelectors.Registration
{
	/// <summary>
	/// Token-to-selector registry consulted by the implicit string conversion operators.
	/// Tokens are resolved by exact name match (the part before the first parenthesis), so
	/// registrations never depend on prefix ordering.
	/// <para>
	/// Selectors in this assembly are discovered automatically through <see cref="SelectorTokenAttribute"/>.
	/// External assemblies defining their own selectors call <see cref="RegisterAssembly"/> once at startup
	/// (or <see cref="Register"/> per token). External selectors must inherit
	/// <see cref="FunctionPropertySelector{T}"/> / <see cref="FunctionPropertySelector"/>, expose public
	/// parameterless and <c>List&lt;PropertySelector&gt;</c> constructors, and use a token starting with
	/// <see cref="SelectorDefaults.FunctionStart"/> — prefer a vendor-scoped name (e.g. <c>$FN_ACME_DISTANCE</c>)
	/// to avoid collisions; duplicate tokens are rejected with an exception.
	/// </para>
	/// </summary>
	public static class SelectorRegistry
	{
		private static readonly ConcurrentDictionary<string, SelectorRegistration> Entries = new(StringComparer.Ordinal);

		private static readonly Lazy<bool> OwnAssemblyScan = new(() =>
		{
			RegisterAssembly(typeof(SelectorRegistry).Assembly);
			return true;
		}, LazyThreadSafetyMode.ExecutionAndPublication);

		/// <summary>
		/// Registers the selector type(s) handling <paramref name="token"/>. Either type may be null;
		/// a null slot of an existing entry is filled on a later call. Registering the same type twice
		/// is a no-op; registering a different type for an occupied slot throws.
		/// </summary>
		public static void Register(string token, Type? openGenericSelectorType, Type? nonGenericSelectorType)
		{
			if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(SelectorDefaults.FunctionStart))
				throw new ArgumentException($"Selector tokens must start with '{SelectorDefaults.FunctionStart}'. Token: '{token}'.", nameof(token));

			if (openGenericSelectorType is not null
				&& (!openGenericSelectorType.IsGenericTypeDefinition || openGenericSelectorType.GetGenericArguments().Length != 1))
				throw new ArgumentException($"The provided type {openGenericSelectorType.FullName} is not an open generic selector type of arity 1.", nameof(openGenericSelectorType));

			if (nonGenericSelectorType is not null && !nonGenericSelectorType.IsAssignableTo(typeof(PropertySelector)))
				throw new ArgumentException($"The provided type {nonGenericSelectorType.FullName} is not a PropertySelector.", nameof(nonGenericSelectorType));

			Entries.AddOrUpdate(token,
				_ => new SelectorRegistration(openGenericSelectorType, nonGenericSelectorType),
				(_, existing) => new SelectorRegistration(
					MergeSlot(token, existing.OpenGenericSelectorType, openGenericSelectorType),
					MergeSlot(token, existing.NonGenericSelectorType, nonGenericSelectorType)));
		}

		/// <summary>
		/// Scans <paramref name="assembly"/> for types decorated with <see cref="SelectorTokenAttribute"/>
		/// and registers them. Called automatically for this assembly on first resolution; external
		/// assemblies defining their own selectors call this once at startup.
		/// Note: relies on <see cref="Assembly.GetTypes"/> and is not trimming/AOT friendly.
		/// </summary>
		public static void RegisterAssembly(Assembly assembly)
		{
			foreach (var type in assembly.GetTypes())
			{
				var attribute = type.GetCustomAttribute<SelectorTokenAttribute>();
				if (attribute is null)
					continue;

				if (type.IsGenericTypeDefinition)
					Register(attribute.Token, type, null);
				else
					Register(attribute.Token, null, type);
			}
		}

		internal static bool TryResolveGeneric(string selectorPart, Type currentIterationType, [NotNullWhen(true)] out PropertySelector? selector)
		{
			selector = null;
			if (!TryGetEntry(selectorPart, out var entry, out var parameters) || entry.OpenGenericSelectorType is null)
				return false;

			selector = CreateSelector(selectorPart, parameters, () => parameters.Count > 0
				? GenericPropertySelectorFactory.CreateGeneric(entry.OpenGenericSelectorType, currentIterationType, parameters)
				: GenericPropertySelectorFactory.CreateGeneric(entry.OpenGenericSelectorType, currentIterationType));
			return true;
		}

		internal static bool TryResolveNonGeneric(string selectorPart, [NotNullWhen(true)] out PropertySelector? selector)
		{
			selector = null;
			if (!TryGetEntry(selectorPart, out var entry, out var parameters) || entry.NonGenericSelectorType is null)
				return false;

			selector = CreateSelector(selectorPart, parameters, () => parameters.Count > 0
				? (PropertySelector)Activator.CreateInstance(entry.NonGenericSelectorType, parameters)!
				: (PropertySelector)Activator.CreateInstance(entry.NonGenericSelectorType)!);
			return true;
		}

		private static PropertySelector CreateSelector(string selectorPart, List<PropertySelector> parameters, Func<PropertySelector> create)
		{
			try
			{
				return create();
			}
			catch (MissingMethodException)
			{
				// The selector type has no constructor for this parameter count (e.g. "$FN_AGGR_SUM(x)").
				throw new NotSupportedException($"The function '{selectorPart}' is not supported with {parameters.Count} parameter(s).");
			}
			catch (TargetInvocationException e) when (e.InnerException is not null)
			{
				// Surface the constructor's own exception (e.g. overload-mismatch ArgumentException) instead of the reflection wrapper.
				ExceptionDispatchInfo.Capture(e.InnerException).Throw();
				throw;
			}
		}

		private static bool TryGetEntry(string selectorPart, [NotNullWhen(true)] out SelectorRegistration? entry, out List<PropertySelector> parameters)
		{
			_ = OwnAssemblyScan.Value;

			var parenthesesIndex = selectorPart.IndexOf('(');
			var name = parenthesesIndex < 0 ? selectorPart : selectorPart[..parenthesesIndex];

			if (!Entries.TryGetValue(name, out entry))
			{
				parameters = [];
				return false;
			}

			PropertySelectorUtils.TryGetParameters(selectorPart, out parameters);
			return true;
		}

		private static Type? MergeSlot(string token, Type? existing, Type? incoming)
		{
			if (incoming is null)
				return existing;
			if (existing is not null && existing != incoming)
				throw new InvalidOperationException($"The token '{token}' is already registered to {existing.FullName} and cannot be re-registered to {incoming.FullName}.");
			return incoming;
		}
	}
}
