using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.PropertySelectors
{
	/// <summary>
	/// A set of member rules that <see cref="MemberFilter.Apply"/> and <see cref="MemberFilter.ApplyFromAssembly"/> add to a
	/// filter. Derive from <see cref="MemberRules{T}"/>.
	/// </summary>
	public abstract class MemberRules
	{
		private protected MemberRules()
		{
		}

		internal abstract MemberFilter ApplyTo(MemberFilter filter);
	}

	/// <summary>The member rules for one type, kept in a class of their own, such as one next to each entity.</summary>
	/// <typeparam name="T">The type the rules apply to, along with types derived from it.</typeparam>
	/// <example>
	/// <code>
	/// public sealed class CustomerMemberRules : MemberRules&lt;Customer&gt;
	/// {
	///     public override void Configure(MemberRulesBuilder&lt;Customer&gt; rules) => rules
	///         .Hide(c => c.PasswordHash)
	///         .Hide(c => c.CreditLimit);
	/// }
	/// </code>
	/// </example>
	public abstract class MemberRules<T> : MemberRules
	{
		/// <summary>Adds the rules for <typeparamref name="T"/>.</summary>
		/// <param name="rules">The builder to add the rules to.</param>
		public abstract void Configure(MemberRulesBuilder<T> rules);

		internal sealed override MemberFilter ApplyTo(MemberFilter filter) => filter.For<T>(Configure);
	}

	/// <summary>
	/// Collects the rules for one type, for <see cref="MemberFilter.For{T}"/> and <see cref="MemberRules{T}.Configure"/>.
	/// Rules add up and the strictest wins: <c>Hide</c>s add up, and two <c>Only</c> lists intersect.
	/// </summary>
	/// <typeparam name="T">The type the rules apply to, along with types derived from it.</typeparam>
	public sealed class MemberRulesBuilder<T>
	{
		private readonly List<(bool Only, HashSet<string> Names)> _rules = [];

		internal MemberRulesBuilder()
		{
		}

		/// <summary>Hides the named members. Hiding a navigation also hides everything behind it.</summary>
		/// <param name="members">The members, each a lambda that reads one property of its parameter, such as <c>x => x.PasswordHash</c>.</param>
		/// <returns>This builder, to chain more rules.</returns>
		/// <exception cref="ArgumentException">No member is named, or a lambda doesn't read one property of its parameter directly.</exception>
		public MemberRulesBuilder<T> Hide(params Expression<Func<T, object?>>[] members)
		{
			_rules.Add((false, Names(members)));
			return this;
		}

		/// <summary>Makes the named members the only ones reachable on <typeparamref name="T"/>.</summary>
		/// <param name="members">The members, each a lambda that reads one property of its parameter, such as <c>x => x.Name</c>.</param>
		/// <returns>This builder, to chain more rules.</returns>
		/// <exception cref="ArgumentException">No member is named, or a lambda doesn't read one property of its parameter directly.</exception>
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
