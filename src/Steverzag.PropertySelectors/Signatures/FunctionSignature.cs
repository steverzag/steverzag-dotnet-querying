using System.Linq.Expressions;

namespace Steverzag.PropertySelectors
{
	public sealed record FunctionSignature(
		IReadOnlyList<ArgumentConstraint> Parameters,
		Func<Type?, IReadOnlyList<Type?>, Type?> ResultType,
		Func<IReadOnlyList<PropertySelector>, Func<Expression?, Expression>> Bind)
	{
		public ArgumentConstraint Target { get; init; } = ArgumentConstraint.Any;

		public ArgumentConstraint? Rest { get; init; }

		public bool Accepts(int argumentCount) => Rest is null ? argumentCount == Parameters.Count : argumentCount >= Parameters.Count;

		public ArgumentConstraint ConstraintAt(int index) => index < Parameters.Count ? Parameters[index] : Rest!;

		public override string ToString()
		{
			var parameters = Parameters.Select(p => p.Description);
			if (Rest is not null)
				parameters = parameters.Append($"{Rest.Description}, …");
			return $"({string.Join(", ", parameters)})";
		}
	}

	internal static class ResultTypes
	{
		internal static Func<Type?, IReadOnlyList<Type?>, Type?> Fixed(Type type) => (_, _) => type;

		internal static Type? ElementOf(Type? collectionType) =>
			collectionType is not null && SelectorExpressions.TryGetEnumerableElementType(collectionType, out var element) ? element : null;
	}
}
