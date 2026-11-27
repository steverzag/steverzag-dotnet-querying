using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Steverzag.PropertySelectors.MetaData;

namespace Steverzag.PropertySelectors
{
	public sealed record MemberContext(Type Type, PropertyInfo Property, PropertyInfo? Source = null, Type? SourceType = null);

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

		public static MemberFilter All { get; } = new(_ => true, isAll: true);

		public static MemberFilter Portable { get; } = new(context => PortableMembers.Allows(context.Type, context.Property) && !PortableMembers.IsChained(context));

		public bool IsAll { get; }

		public bool Allows(Type type, PropertyInfo property)
		{
			ArgumentNullException.ThrowIfNull(type);
			ArgumentNullException.ThrowIfNull(property);
			return _allows(new MemberContext(type, property));
		}

		public bool Allows(MemberContext context)
		{
			ArgumentNullException.ThrowIfNull(context);
			return _allows(context);
		}

		public MemberFilter Except(Func<Type, PropertyInfo, bool> hide)
		{
			ArgumentNullException.ThrowIfNull(hide);
			return Except(context => hide(context.Type, context.Property));
		}

		public MemberFilter Except(Func<MemberContext, bool> hide)
		{
			ArgumentNullException.ThrowIfNull(hide);
			var allows = _allows;
			return new(context => allows(context) && !hide(context));
		}

		public MemberFilter Only(Func<Type, PropertyInfo, bool> allow)
		{
			ArgumentNullException.ThrowIfNull(allow);
			return Only(context => allow(context.Type, context.Property));
		}

		public MemberFilter Only(Func<MemberContext, bool> allow)
		{
			ArgumentNullException.ThrowIfNull(allow);
			var allows = _allows;
			return new(context => allows(context) && allow(context));
		}

		public MemberFilter For<T>(Action<MemberRulesBuilder<T>> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);
			var builder = new MemberRulesBuilder<T>();
			configure(builder);
			return Only(builder.Build());
		}

		public MemberFilter Hide<T>(params Expression<Func<T, object?>>[] members) => For<T>(rules => rules.Hide(members));

		public MemberFilter Only<T>(params Expression<Func<T, object?>>[] members) => For<T>(rules => rules.Only(members));

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

		public MemberFilter ApplyFromAssembly(Assembly assembly)
		{
			ArgumentNullException.ThrowIfNull(assembly);
			var rules = assembly.GetTypes()
				.Where(type => !type.IsAbstract && !type.ContainsGenericParameters && type.IsAssignableTo(typeof(MemberRules)) && type.GetConstructor(Type.EmptyTypes) is not null)
				.OrderBy(type => type.FullName, StringComparer.Ordinal)
				.Select(type => (MemberRules)Activator.CreateInstance(type)!);
			return Apply([.. rules]);
		}

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
