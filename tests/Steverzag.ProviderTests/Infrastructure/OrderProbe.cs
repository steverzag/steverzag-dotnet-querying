using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Steverzag.ProviderTests.Infrastructure
{
	/// <summary>
	/// Orders <see cref="Scalar"/> rows by one column (then by Id) in the database and in memory. <see cref="ProbeOutcome.Error"/>
	/// or <see cref="ProbeOutcome.NotTranslated"/> means the provider can't sort that type at all; <see cref="ProbeOutcome.WrongRows"/>
	/// means it sorts, but in a different order from .NET (e.g. SQL Server's <c>uniqueidentifier</c> order).
	/// </summary>
	public static class OrderProbe
	{
		public static IEnumerable<string> Columns()
			=> typeof(Scalar).GetProperties().Where(p => p.Name != nameof(Scalar.Id)).Select(p => p.Name);

		public static Type ColumnType(string column)
		{
			var type = typeof(Scalar).GetProperty(column)!.PropertyType;
			return Nullable.GetUnderlyingType(type) ?? type;
		}

		public static ProbeResult Run(ProviderFixture fixture, string column)
		{
			try
			{
				var parameter = Expression.Parameter(typeof(Scalar), "s");
				var body = Expression.Property(parameter, column);
				var read = Expression.Lambda<Func<Scalar, object?>>(Expression.Convert(body, typeof(object)), parameter).Compile();
				var expected = fixture.Scalars.OrderBy(read, InMemoryComparer.Instance).ThenBy(s => s.Id).Select(s => s.Id).ToList();

				using var context = fixture.CreateContext();
				IQueryable<Scalar> scalars = context.Scalars;
				var ordered = scalars.Provider.CreateQuery<Scalar>(Expression.Call(
					typeof(Queryable), nameof(Queryable.OrderBy), [typeof(Scalar), body.Type],
					scalars.Expression, Expression.Quote(Expression.Lambda(body, parameter))));
				var actual = ((IOrderedQueryable<Scalar>)ordered).ThenBy(s => s.Id).Select(s => s.Id).ToList();

				return actual.SequenceEqual(expected)
					? new(column, ProbeOutcome.Ok)
					: new(column, ProbeOutcome.WrongRows, $"expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
			}
			catch (InvalidOperationException ex) when (ex.Message.Contains("could not be translated"))
			{
				return new(column, ProbeOutcome.NotTranslated);
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				var end = message.IndexOfAny(['\r', '\n']);
				return new(column, ProbeOutcome.Error, $"{ex.GetType().Name}: {(end < 0 ? message : message[..end])}");
			}
		}

		private sealed class InMemoryComparer : IComparer<object?>
		{
			public static readonly InMemoryComparer Instance = new();

			public int Compare(object? x, object? y) => (x, y) switch
			{
				(null, null) => 0,
				(null, _) => -1,
				(_, null) => 1,
				(string a, string b) => string.CompareOrdinal(a, b),
				(byte[] a, byte[] b) => a.AsSpan().SequenceCompareTo(b),
				_ => Comparer<object>.Default.Compare(x, y),
			};
		}
	}
}
