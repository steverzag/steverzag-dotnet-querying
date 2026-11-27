using System.Linq.Expressions;
using System.Reflection;
using Steverzag.PropertySelectors;

namespace Steverzag.ProviderTests.Infrastructure
{
	public enum ProbeOutcome
	{
		Ok,
		WrongRows,
		NotTranslated,
		Error,
	}

	public sealed record ProbeResult(string Selector, ProbeOutcome Outcome, string? Detail = null);

	/// <summary>
	/// Filters on one member with <c>member == value</c>, once per distinct value in the seed, and checks the database
	/// returns the same rows as the same expression in memory. A filter is used rather than a projection because EF Core
	/// silently evaluates an untranslatable final projection on the client, but throws for an untranslatable filter.
	/// </summary>
	public static class MemberProbe
	{
		public static readonly Type[] FrameworkTypes = [typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan)];

		/// <summary>
		/// Members whose value depends on the time zone: in memory the app machine's, in SQL the database session's. A probe
		/// passes only where the two happen to match (CI runs everything in UTC), so no preset may allow them.
		/// </summary>
		public static readonly PropertyInfo[] ZoneDependent = [typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.LocalDateTime))!];

		public static IEnumerable<PropertyInfo> Members(Type type)
			=> type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0).OrderBy(p => p.Name);

		public static IEnumerable<string> Columns(Type type)
			=> typeof(Moment).GetProperties()
				.Where(p => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) == type)
				.Select(p => p.Name);

		public static IEnumerable<(Type Type, PropertyInfo Member, string Selector)> Selectors()
			=> from type in FrameworkTypes
			   from member in Members(type)
			   from column in Columns(type)
			   select (type, member, $"{column}.{member.Name}");

		public static ProbeResult Run(ProviderFixture fixture, string selectorText)
		{
			try
			{
				PropertySelector<Moment> selector = selectorText;
				var parameter = Expression.Parameter(typeof(Moment), "m");
				var body = SelectorExpressions.CreateMemberAccess(parameter, selector);
				var read = Expression.Lambda<Func<Moment, object?>>(Expression.Convert(body, typeof(object)), parameter).Compile();

				using var context = fixture.CreateContext();
				foreach (var group in fixture.Rows.GroupBy(read))
				{
					var expected = group.Select(m => m.Id).Order().ToList();
					var value = group.Key is null
						? (Expression)Expression.Constant(null, body.Type)
						: Expression.Property(Expression.Constant(Activator.CreateInstance(typeof(Holder<>).MakeGenericType(body.Type), group.Key)), "Value");
					var where = Expression.Lambda<Func<Moment, bool>>(Expression.Equal(body, value), parameter);

					var actual = context.Moments.Where(where).Select(m => m.Id).OrderBy(id => id).ToList();
					if (!actual.SequenceEqual(expected))
						return new(selectorText, ProbeOutcome.WrongRows, $"= {group.Key}: expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
				}

				return new(selectorText, ProbeOutcome.Ok);
			}
			catch (InvalidOperationException ex) when (ex.Message.Contains("could not be translated"))
			{
				return new(selectorText, ProbeOutcome.NotTranslated);
			}
			catch (Exception ex)
			{
				return new(selectorText, ProbeOutcome.Error, $"{ex.GetType().Name}: {FirstLine(ex.Message)}");
			}
		}

		public static readonly ExpressionType[] Comparisons = [ExpressionType.Equal, ExpressionType.GreaterThan, ExpressionType.LessThan];

		/// <summary>
		/// Compares the whole value of a column with each distinct seeded value (<c>==</c>, <c>&gt;</c>, <c>&lt;</c>), or, for
		/// <see langword="null"/> <paramref name="comparison"/>, orders by it. Ordering is checked on plain columns only, since
		/// providers put nulls first or last.
		/// </summary>
		public static ProbeResult RunValue(ProviderFixture fixture, string column, ExpressionType? comparison)
		{
			var label = comparison is { } c ? $"{column} {c}" : $"{column} OrderBy";
			try
			{
				var parameter = Expression.Parameter(typeof(Moment), "m");
				var body = Expression.Property(parameter, column);
				using var context = fixture.CreateContext();

				if (comparison is null)
				{
					var key = Expression.Lambda<Func<Moment, object?>>(Expression.Convert(body, typeof(object)), parameter);
					var expected = fixture.Rows.OrderBy(key.Compile()).ThenBy(m => m.Id).Select(m => m.Id).ToList();
					IQueryable<Moment> moments = context.Moments;
					var ordered = moments.Provider.CreateQuery<Moment>(Expression.Call(
						typeof(Queryable), nameof(Queryable.OrderBy), [typeof(Moment), body.Type],
						moments.Expression, Expression.Quote(Expression.Lambda(body, parameter))));
					var actual = ordered.Select(m => m.Id).ToList();
					return actual.SequenceEqual(expected)
						? new(label, ProbeOutcome.Ok)
						: new(label, ProbeOutcome.WrongRows, $"expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
				}

				foreach (var row in fixture.Rows)
				{
					var value = typeof(Moment).GetProperty(column)!.GetValue(row);
					if (value is null)
						continue;

					var holder = Expression.Property(Expression.Constant(Activator.CreateInstance(typeof(Holder<>).MakeGenericType(body.Type), value)), "Value");
					var where = Expression.Lambda<Func<Moment, bool>>(Expression.MakeBinary(comparison.Value, body, holder), parameter);
					var expected = fixture.Rows.Where(where.Compile()).Select(m => m.Id).Order().ToList();
					var actual = context.Moments.Where(where).Select(m => m.Id).OrderBy(id => id).ToList();
					if (!actual.SequenceEqual(expected))
						return new(label, ProbeOutcome.WrongRows, $"vs {value}: expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
				}

				return new(label, ProbeOutcome.Ok);
			}
			catch (InvalidOperationException ex) when (ex.Message.Contains("could not be translated"))
			{
				return new(label, ProbeOutcome.NotTranslated);
			}
			catch (Exception ex)
			{
				return new(label, ProbeOutcome.Error, $"{ex.GetType().Name}: {FirstLine(ex.Message)}");
			}
		}

		public static IEnumerable<string> AllColumns()
			=> FrameworkTypes.SelectMany(Columns);

		private static string FirstLine(string message)
		{
			var end = message.IndexOfAny(['\r', '\n']);
			return end < 0 ? message : message[..end];
		}

		internal sealed class Holder<T>(T value)
		{
			public T Value { get; } = value;
		}
	}
}
