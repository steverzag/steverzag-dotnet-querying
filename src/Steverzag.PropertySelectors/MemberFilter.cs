using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Steverzag.PropertySelectors.MetaData;

namespace Steverzag.PropertySelectors
{
	/// <summary>A member a selector reads, and where its value came from, as a <see cref="MemberFilter"/> predicate sees it.</summary>
	/// <param name="Type">
	/// The type the member is read on: the underlying type for a nullable value, and the element type for a collection
	/// projection.
	/// </param>
	/// <param name="Property">The member.</param>
	/// <param name="Source">
	/// The property the value was read from, or <see langword="null"/> when the member is read on the root, on a function
	/// result or on a literal. It carries across <c>|</c> and out of a parenthesized group.
	/// </param>
	/// <param name="SourceType">The type <paramref name="Source"/> was read on, or <see langword="null"/> when there is no source.</param>
	public sealed record MemberContext(Type Type, PropertyInfo Property, PropertyInfo? Source = null, Type? SourceType = null);

	/// <summary>
	/// Decides which members a selector may reach. The same filter hides members from <see cref="PropertiesExplorer{T}"/>
	/// and rejects them when <see cref="Parse{T}"/> parses a selector.
	/// </summary>
	/// <remarks>
	/// A filter is immutable: every method returns a new one, and rules compose so the strictest wins. A hidden member is
	/// reported exactly like a missing one, so a client can't tell that it exists; the real reason is the error's
	/// <see cref="Exception.InnerException"/>. See "Member filters" in <c>docs/property-selectors.md</c>.
	/// </remarks>
	/// <example>
	/// <code>
	/// var filter = MemberFilter.Portable.Hide&lt;Person&gt;(p => p.PasswordHash);
	/// new PropertiesExplorer&lt;Person&gt;().ExplorePropertiesTree(4, filter); // PasswordHash is left out
	/// filter.Parse&lt;Person&gt;("PasswordHash");                              // SelectorParseException: not found
	/// </code>
	/// </example>
	public sealed class MemberFilter
	{
		private readonly Func<MemberContext, bool> _allows;

		[ThreadStatic]
		private static MemberFilter? _active;

		[ThreadStatic]
		private static (PropertyInfo Property, Type Type)? _pipedSource;

		private static readonly ConditionalWeakTable<PropertySelector, Tuple<PropertyInfo, Type>> LastMembers = new();

		private MemberFilter(Func<MemberContext, bool> allows, bool isAll = false)
		{
			_allows = allows;
			IsAll = isAll;
		}

		/// <summary>A filter that allows every member. It is the default wherever a filter is optional.</summary>
		public static MemberFilter All { get; } = new(_ => true, isAll: true);

		/// <summary>
		/// A filter that restricts only the framework date and time types, to the members that filter correctly on SQLite,
		/// SQL Server and PostgreSQL. Your own types are left alone.
		/// </summary>
		/// <remarks>
		/// <see cref="DateTime"/> allows <c>Year</c>, <c>Month</c>, <c>Day</c>, <c>Hour</c>, <c>Minute</c>, <c>Date</c> and
		/// <c>DayOfYear</c>; <see cref="DateOnly"/> allows <c>Year</c>, <c>Month</c>, <c>Day</c> and <c>DayOfYear</c>;
		/// <see cref="DateTimeOffset"/>, <see cref="TimeOnly"/> and <see cref="TimeSpan"/> allow none. A date or time member
		/// can't be read from a value another one produced, so <c>CreatedAt.Date</c> is allowed but
		/// <c>CreatedAt.Date.Hour</c> isn't.
		/// </remarks>
		public static MemberFilter Portable { get; } = new(context => PortableMembers.Allows(context.Type, context.Property) && !PortableMembers.IsChained(context));

		/// <summary>Whether this filter is <see cref="All"/>, which allows every member.</summary>
		public bool IsAll { get; }

		/// <summary>Whether the filter allows a member read on the root, with no source.</summary>
		/// <param name="type">The type the member is read on.</param>
		/// <param name="property">The member.</param>
		/// <returns><see langword="true"/> if the member is allowed.</returns>
		public bool Allows(Type type, PropertyInfo property)
		{
			ArgumentNullException.ThrowIfNull(type);
			ArgumentNullException.ThrowIfNull(property);
			return _allows(new MemberContext(type, property));
		}

		/// <summary>Whether the filter allows a member, given where its value came from.</summary>
		/// <param name="context">The member and its source.</param>
		/// <returns><see langword="true"/> if the member is allowed.</returns>
		public bool Allows(MemberContext context)
		{
			ArgumentNullException.ThrowIfNull(context);
			return _allows(context);
		}

		/// <summary>Returns a filter that also hides every member the predicate matches.</summary>
		/// <param name="hide">
		/// Receives the type the member is read on and the member, and returns <see langword="true"/> to hide it. A predicate
		/// that checks only the name applies to every type with a member of that name.
		/// </param>
		/// <returns>A new filter; this one is unchanged.</returns>
		/// <example><code>MemberFilter.All.Except((_, p) => p.IsDefined(typeof(SensitiveAttribute)))</code></example>
		public MemberFilter Except(Func<Type, PropertyInfo, bool> hide)
		{
			ArgumentNullException.ThrowIfNull(hide);
			return Except(context => hide(context.Type, context.Property));
		}

		/// <summary>Returns a filter that also hides every member the predicate matches, which can look at the member's source.</summary>
		/// <param name="hide">Receives the member and where its value came from, and returns <see langword="true"/> to hide it.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		/// <example><code>MemberFilter.All.Except(c => c.Source?.Name == "Address") // everything behind any Address property</code></example>
		public MemberFilter Except(Func<MemberContext, bool> hide)
		{
			ArgumentNullException.ThrowIfNull(hide);
			var allows = _allows;
			return new(context => allows(context) && !hide(context));
		}

		/// <summary>Returns a filter that allows only the members the predicate matches, among those this one allows.</summary>
		/// <param name="allow">Receives the type the member is read on and the member, and returns <see langword="true"/> to allow it.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		public MemberFilter Only(Func<Type, PropertyInfo, bool> allow)
		{
			ArgumentNullException.ThrowIfNull(allow);
			return Only(context => allow(context.Type, context.Property));
		}

		/// <summary>
		/// Returns a filter that allows only the members the predicate matches, among those this one allows. The predicate
		/// can look at the member's source.
		/// </summary>
		/// <param name="allow">Receives the member and where its value came from, and returns <see langword="true"/> to allow it.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		public MemberFilter Only(Func<MemberContext, bool> allow)
		{
			ArgumentNullException.ThrowIfNull(allow);
			var allows = _allows;
			return new(context => allows(context) && allow(context));
		}

		/// <summary>
		/// Returns a filter with several rules for one type, named with lambdas. The rules cover <typeparamref name="T"/> and
		/// types derived from it; other types are left alone.
		/// </summary>
		/// <typeparam name="T">The type the rules apply to.</typeparam>
		/// <param name="configure">Adds the rules, such as <c>r => r.Hide(x => x.A).Only(x => x.B, x => x.C)</c>.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		/// <exception cref="ArgumentException">A lambda doesn't read one property of its parameter directly.</exception>
		public MemberFilter For<T>(Action<MemberRulesBuilder<T>> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);
			var builder = new MemberRulesBuilder<T>();
			configure(builder);
			return Only(builder.Build());
		}

		/// <summary>
		/// Returns a filter that also hides the named members of <typeparamref name="T"/>. Hiding a navigation also hides
		/// everything behind it.
		/// </summary>
		/// <typeparam name="T">The type whose members are hidden, along with types derived from it.</typeparam>
		/// <param name="members">The members, each a lambda that reads one property of its parameter, such as <c>x => x.PasswordHash</c>.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		/// <exception cref="ArgumentException">No member is named, or a lambda doesn't read one property of its parameter directly.</exception>
		public MemberFilter Hide<T>(params Expression<Func<T, object?>>[] members) => For<T>(rules => rules.Hide(members));

		/// <summary>
		/// Returns a filter in which the named members are the only ones reachable on <typeparamref name="T"/>. Other types
		/// are left alone.
		/// </summary>
		/// <typeparam name="T">The type whose members are restricted, along with types derived from it.</typeparam>
		/// <param name="members">The members, each a lambda that reads one property of its parameter, such as <c>x => x.Name</c>.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		/// <exception cref="ArgumentException">No member is named, or a lambda doesn't read one property of its parameter directly.</exception>
		public MemberFilter Only<T>(params Expression<Func<T, object?>>[] members) => For<T>(rules => rules.Only(members));

		/// <summary>Returns a filter with the rules of each <see cref="MemberRules"/> class added.</summary>
		/// <param name="rules">The rules, such as one class per entity.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		public MemberFilter Apply(params MemberRules[] rules)
		{
			ArgumentNullException.ThrowIfNull(rules);
			var filter = this;
			foreach (var rule in rules)
			{
				ArgumentNullException.ThrowIfNull(rule, nameof(rules));
				filter = rule.ApplyTo(filter);
			}
			return filter;
		}

		/// <summary>
		/// Returns a filter with the rules of every concrete <see cref="MemberRules{T}"/> class in an assembly that has a
		/// public parameterless constructor.
		/// </summary>
		/// <param name="assembly">The assembly to scan, such as <c>typeof(Program).Assembly</c>.</param>
		/// <returns>A new filter; this one is unchanged.</returns>
		public MemberFilter ApplyFromAssembly(Assembly assembly)
		{
			ArgumentNullException.ThrowIfNull(assembly);
			var rules = assembly.GetTypes()
				.Where(type => !type.IsAbstract && !type.ContainsGenericParameters && type.IsAssignableTo(typeof(MemberRules)) && type.GetConstructor(Type.EmptyTypes) is not null)
				.OrderBy(type => type.FullName, StringComparer.Ordinal)
				.Select(type => (MemberRules)Activator.CreateInstance(type)!);
			return Apply([.. rules]);
		}

		/// <summary>
		/// Parses a selector in the generic form and checks every property it reads against this filter: in the head, after
		/// a pipe, in function arguments and in arithmetic.
		/// </summary>
		/// <remarks>The filter applies only here. A plain parse and the non-generic form don't check members.</remarks>
		/// <typeparam name="T">The type the selector starts from.</typeparam>
		/// <param name="selector">The selector text.</param>
		/// <returns>The parsed selector.</returns>
		/// <exception cref="SelectorParseException">
		/// The selector reads a hidden member, reported as "not found", or it's invalid for another reason, which is reported first.
		/// </exception>
		public PropertySelector<T> Parse<T>(string selector)
		{
			var previous = _active;
			var previousSource = _pipedSource;
			_active = IsAll ? null : this;
			_pipedSource = null;
			try
			{
				return selector;
			}
			finally
			{
				_active = previous;
				_pipedSource = previousSource;
			}
		}

		internal static bool IsActive => _active is not null;

		internal static void PipeFrom(PropertySelector? previous)
		{
			if (_active is null)
				return;

			_pipedSource = previous is not null && LastMembers.TryGetValue(previous, out var last) ? (last.Item1, last.Item2) : null;
		}

		internal static (PropertyInfo Property, Type Type)? TakePipedSource()
		{
			var source = _pipedSource;
			_pipedSource = null;
			return source;
		}

		internal static void Remember(PropertySelector selector, PropertyInfo property, Type type)
		{
			if (_active is not null)
				LastMembers.AddOrUpdate(selector, Tuple.Create(property, type));
		}

		internal static void Check(Type type, PropertyInfo property, (PropertyInfo Property, Type Type)? source, string propertyPath, int offset)
		{
			if (_active is { } filter && !filter._allows(new MemberContext(type, property, source?.Property, source?.Type)))
			{
				var reason = source is { } from && filter._allows(new MemberContext(type, property))
					? $"Property '{property.Name}' on '{TypeDisplay.Name(type)}' is not allowed after '{from.Property.Name}'."
					: $"Property '{property.Name}' on '{TypeDisplay.Name(type)}' is not allowed.";
				throw Extensions.TypesExtensions.PropertyNotFound(property.Name, type, propertyPath, offset,
					new SelectorParseException(reason, propertyPath, offset, property.Name), source);
			}
		}

		internal static bool Hides(Type type, PropertyInfo property, (PropertyInfo Property, Type Type)? source)
			=> _active is { } filter && !filter._allows(new MemberContext(type, property, source?.Property, source?.Type));
	}

	internal static class PortableMembers
	{
		private static readonly Dictionary<Type, HashSet<string>> Members = new()
		{
			[typeof(DateTime)] =
			[
				nameof(DateTime.Year), nameof(DateTime.Month), nameof(DateTime.Day),
				nameof(DateTime.Hour), nameof(DateTime.Minute), nameof(DateTime.Date), nameof(DateTime.DayOfYear),
			],
			[typeof(DateTimeOffset)] = [],
			[typeof(DateOnly)] = [nameof(DateOnly.Year), nameof(DateOnly.Month), nameof(DateOnly.Day), nameof(DateOnly.DayOfYear)],
			[typeof(TimeOnly)] = [],
			[typeof(TimeSpan)] = [],
		};

		internal static IReadOnlyDictionary<Type, HashSet<string>> AllowedMembers => Members;

		internal static bool Allows(Type type, PropertyInfo property)
			=> !Members.TryGetValue(type, out var names) || names.Contains(property.Name);

		internal static bool IsChained(MemberContext context)
			=> context.SourceType is { } sourceType && Members.ContainsKey(sourceType) && Members.ContainsKey(context.Type);
	}
}
