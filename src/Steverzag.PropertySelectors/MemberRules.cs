using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.PropertySelectors
{
	public abstract class MemberRules
	{
		private protected MemberRules()
		{
		}

		internal abstract MemberFilter ApplyTo(MemberFilter filter);
	}

	public abstract class MemberRules<T> : MemberRules
	{
		public abstract void Configure(MemberRulesBuilder<T> rules);

		internal sealed override MemberFilter ApplyTo(MemberFilter filter) => filter.For<T>(Configure);
	}

	public sealed class MemberRulesBuilder<T>
	{
		private readonly List<(bool Only, HashSet<string> Names)> _rules = [];

		internal MemberRulesBuilder()
		{
		}

		public MemberRulesBuilder<T> Hide(params Expression<Func<T, object?>>[] members)
		{
			_rules.Add((false, Names(members)));
			return this;
		}

		public MemberRulesBuilder<T> Only(params Expression<Func<T, object?>>[] members)
		{
			_rules.Add((true, Names(members)));
			return this;
		}

		internal Func<Type, PropertyInfo, bool> Build()
		{
			var rules = _rules.ToArray();
			return (type, property) =>
			{
				if (!type.IsAssignableTo(typeof(T)))
					return true;

				foreach (var (only, names) in rules)
				{
					if (names.Contains(property.Name) != only)
						return false;
				}
				return true;
			};
		}

		private static HashSet<string> Names(Expression<Func<T, object?>>[] members)
		{
			ArgumentNullException.ThrowIfNull(members);
			if (members.Length == 0)
				throw new ArgumentException($"Name at least one property of {typeof(T).Name}.", nameof(members));

			return [.. members.Select(PropertyName)];
		}

		private static string PropertyName(Expression<Func<T, object?>> member)
		{
			ArgumentNullException.ThrowIfNull(member);

			var body = member.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
				? convert.Operand
				: member.Body;

			if (body is MemberExpression { Member: PropertyInfo property } access && access.Expression == member.Parameters[0])
				return property.Name;

			var hint = body is MemberExpression { Expression: MemberExpression { Member: PropertyInfo inner } }
				? $" To hide a member of a nested type, use For<{inner.PropertyType.Name}>."
				: string.Empty;

			throw new ArgumentException(
				$"Expected a property of {typeof(T).Name}, like 'x => x.Name': '{member}'.{hint}", nameof(member));
		}
	}
}
