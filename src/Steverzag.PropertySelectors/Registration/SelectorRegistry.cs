using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Steverzag.PropertySelectors.Registration
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
		private static readonly ConcurrentDictionary<Type, Func<string, PropertySelector>> GenericArgumentParserCache = new();

		private static readonly Lazy<bool> OwnAssemblyScan = new(() =>
		{
			RegisterAssembly(typeof(SelectorRegistry).Assembly);
			return true;
		}, LazyThreadSafetyMode.ExecutionAndPublication);

		private sealed record SelectorFactories(
			Func<PropertySelector>? CreateParameterless,
			Func<List<PropertySelector>, PropertySelector>? CreateWithParameters,
			PipedForm Piped,
			string? PipedHint,
			ArgumentScope Arguments);

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

		internal static bool TryResolveGeneric(string selectorPart, Type currentIterationType, Type rootType, string? pipedText, [NotNullWhen(true)] out PropertySelector? selector)
		{
			selector = null;
			if (!TryGetEntry(selectorPart, out var entry) || entry.OpenGenericSelectorType is null)
				return false;

			var factories = GenericFactoryCache.GetOrAdd(
				(entry.OpenGenericSelectorType, currentIterationType),
				static key => BuildFactories(key.OpenGenericType.MakeGenericType(key.ElementType)));

			var argumentScopeType = factories.Arguments switch
			{
				ArgumentScope.TargetElement => ExpressionUtils.TryGetEnumerableElementType(currentIterationType, out var elementType) ? elementType : null,
				_ => rootType,
			};

			ExceptionDispatchInfo? argumentError = null;
			var parameters = argumentScopeType is null
				? ParseParameters(selectorPart, ParseNonGeneric)
				: ParseParameters(selectorPart, (text, offset) =>
				{
					try
					{
						return PropertySelectorUtils.ParseArgument(GenericArgumentParser(argumentScopeType), selectorPart, text, offset);
					}
					catch (Exception e) when (e is not SelectorParseException { IsLimit: true })
					{
						argumentError ??= ExceptionDispatchInfo.Capture(e);
						return PropertySelectorUtils.ParseArgument(ParseNonGeneric, selectorPart, text, offset);
					}
				});

			ApplyPipedForm(selectorPart, pipedText, currentIterationType, parameters, factories);

			selector = CreateSelector(selectorPart, parameters, factories, pipedText is not null);
			argumentError?.Throw();
			return true;
		}

		internal static bool TryResolveNonGeneric(string selectorPart, string? pipedText, [NotNullWhen(true)] out PropertySelector? selector)
		{
			selector = null;
			if (!TryGetEntry(selectorPart, out var entry) || entry.NonGenericSelectorType is null)
				return false;

			var factories = NonGenericFactoryCache.GetOrAdd(entry.NonGenericSelectorType, static type => BuildFactories(type));

			var parameters = ParseParameters(selectorPart, ParseNonGeneric);

			ApplyPipedForm(selectorPart, pipedText, null, parameters, factories);

			selector = CreateSelector(selectorPart, parameters, factories, pipedText is not null);
			return true;
		}

		private static void ApplyPipedForm(string selectorPart, string? pipedText, Type? pipedType, List<PropertySelector> parameters, SelectorFactories factories)
		{
			if (pipedText is null)
				return;

			switch (factories.Piped)
			{
				case PipedForm.FirstArgument:
					parameters.Insert(0, new PipedValueSelector(pipedType, pipedText));
					break;
				case PipedForm.Rejected:
					var name = TokenName(selectorPart);
					var message = $"{name} takes no piped value, but '{pipedText}' is piped into it. Write it at the start of a chain: '{selectorPart}'.";
					throw new ArgumentException(factories.PipedHint is null ? message : $"{message} {factories.PipedHint}");
			}
		}

		private static string TokenName(string selectorPart) => PropertySelectorUtils.FunctionName(selectorPart);

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

			var attribute = closedSelectorType.GetCustomAttribute<SelectorTokenAttribute>();

			return new SelectorFactories(createParameterless, createWithParameters,
				attribute?.Piped ?? PipedForm.Target, attribute?.PipedHint, attribute?.Arguments ?? ArgumentScope.Root);
		}

		private static PropertySelector CreateSelector(string selectorPart, List<PropertySelector> parameters, SelectorFactories factories, bool piped)
		{
			var selector = Instantiate(selectorPart, parameters, factories);

			if (selector is IPipeableFunctionSelector function)
				function.IsPiped = piped;

			return selector;
		}

		private static PropertySelector Instantiate(string selectorPart, List<PropertySelector> parameters, SelectorFactories factories)
		{
			if (parameters.Count > 0)
			{
				return factories.CreateWithParameters is not null
					? factories.CreateWithParameters(parameters)
					: throw new SelectorParseException($"{TokenName(selectorPart)} takes no arguments.", selectorPart, selectorPart.IndexOf('('), TokenName(selectorPart));
			}

			return factories.CreateParameterless is not null
				? factories.CreateParameterless()
				: throw new SelectorParseException($"{TokenName(selectorPart)} needs arguments.", selectorPart, 0, TokenName(selectorPart));
		}

		internal static bool IsRegistered(string token) => TryGetEntry(token, out _);

		internal static string? FindTokenIgnoringCase(string token)
		{
			Initialize();

			return Entries.Keys.FirstOrDefault(key => string.Equals(key, token, StringComparison.OrdinalIgnoreCase));
		}

		private static bool TryGetEntry(string selectorPart, [NotNullWhen(true)] out SelectorRegistration? entry)
		{
			Initialize();

			return Entries.TryGetValue(TokenName(selectorPart), out entry);
		}

		private static List<PropertySelector> ParseParameters(string selectorPart, Func<string, PropertySelector> parse)
			=> ParseParameters(selectorPart, (text, offset) => PropertySelectorUtils.ParseArgument(parse, selectorPart, text, offset));

		private static List<PropertySelector> ParseParameters(string selectorPart, Func<string, int, PropertySelector> parse)
		{
			PropertySelectorUtils.TryGetParameters(selectorPart, parse, out var parameters);
			return parameters;
		}

		private static PropertySelector ParseNonGeneric(string text) => (PropertySelector)text;

		private static PropertySelector ParseGeneric<TScope>(string text) => (PropertySelector<TScope>)text;

		private static readonly MethodInfo ParseGenericMethod =
			typeof(SelectorRegistry).GetMethod(nameof(ParseGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

		private static Func<string, PropertySelector> GenericArgumentParser(Type scopeType) =>
			GenericArgumentParserCache.GetOrAdd(scopeType, static type =>
				ParseGenericMethod.MakeGenericMethod(type).CreateDelegate<Func<string, PropertySelector>>());

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
