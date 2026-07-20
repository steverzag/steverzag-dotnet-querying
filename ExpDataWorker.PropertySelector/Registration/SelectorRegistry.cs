using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

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
	/// <para>
	/// Reflection is paid once per selector/element-type combination: instantiation goes through
	/// compiled constructor delegates cached per closed type, so repeated conversions do not
	/// touch reflection at all.
	/// </para>
	/// </summary>
	public static class SelectorRegistry
	{
		private static readonly ConcurrentDictionary<string, SelectorRegistration> Entries = new(StringComparer.Ordinal);

		private static readonly ConcurrentDictionary<(Type OpenGenericType, Type ElementType), SelectorFactories> GenericFactoryCache = new();
		private static readonly ConcurrentDictionary<Type, SelectorFactories> NonGenericFactoryCache = new();

		private static readonly Lazy<bool> OwnAssemblyScan = new(() =>
		{
			RegisterAssembly(typeof(SelectorRegistry).Assembly);
			return true;
		}, LazyThreadSafetyMode.ExecutionAndPublication);

		private sealed record SelectorFactories(
			Func<PropertySelector>? CreateParameterless,
			Func<List<PropertySelector>, PropertySelector>? CreateWithParameters);

		/// <summary>
		/// Forces the one-time scan of this assembly's selectors. Calling it is optional — the first
		/// string conversion triggers the scan lazily — but applications sensitive to first-request
		/// latency can invoke it during startup to pay the cost eagerly. Idempotent and thread-safe.
		/// </summary>
		public static void Initialize()
		{
			_ = OwnAssemblyScan.Value;
		}

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

			var factories = GenericFactoryCache.GetOrAdd(
				(entry.OpenGenericSelectorType, currentIterationType),
				static key => BuildFactories(key.OpenGenericType.MakeGenericType(key.ElementType)));

			selector = CreateSelector(selectorPart, parameters, factories);
			return true;
		}

		internal static bool TryResolveNonGeneric(string selectorPart, [NotNullWhen(true)] out PropertySelector? selector)
		{
			selector = null;
			if (!TryGetEntry(selectorPart, out var entry, out var parameters) || entry.NonGenericSelectorType is null)
				return false;

			var factories = NonGenericFactoryCache.GetOrAdd(entry.NonGenericSelectorType, static type => BuildFactories(type));

			selector = CreateSelector(selectorPart, parameters, factories);
			return true;
		}

		private static SelectorFactories BuildFactories(Type closedSelectorType)
		{
			if (!closedSelectorType.IsAssignableTo(typeof(PropertySelector)))
				throw new ArgumentException($"The provided type {closedSelectorType.FullName} is not a PropertySelector.");

			Func<PropertySelector>? createParameterless = null;
			var parameterlessConstructor = closedSelectorType.GetConstructor(Type.EmptyTypes);
			if (parameterlessConstructor is not null)
			{
				createParameterless = Expression.Lambda<Func<PropertySelector>>(
					Expression.New(parameterlessConstructor)).Compile();
			}

			Func<List<PropertySelector>, PropertySelector>? createWithParameters = null;
			var parametersConstructor = closedSelectorType.GetConstructor([typeof(List<PropertySelector>)]);
			if (parametersConstructor is not null)
			{
				var parametersArgument = Expression.Parameter(typeof(List<PropertySelector>), "parameters");
				createWithParameters = Expression.Lambda<Func<List<PropertySelector>, PropertySelector>>(
					Expression.New(parametersConstructor, parametersArgument), parametersArgument).Compile();
			}

			return new SelectorFactories(createParameterless, createWithParameters);
		}

		private static PropertySelector CreateSelector(string selectorPart, List<PropertySelector> parameters, SelectorFactories factories)
		{
			if (parameters.Count > 0)
			{
				return factories.CreateWithParameters is not null
					? factories.CreateWithParameters(parameters)
					: throw new NotSupportedException($"The function '{selectorPart}' is not supported with {parameters.Count} parameter(s).");
			}

			return factories.CreateParameterless is not null
				? factories.CreateParameterless()
				: throw new NotSupportedException($"The function '{selectorPart}' is not supported without parameters.");
		}

		private static bool TryGetEntry(string selectorPart, [NotNullWhen(true)] out SelectorRegistration? entry, out List<PropertySelector> parameters)
		{
			Initialize();

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
