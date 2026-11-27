using System.Linq.Expressions;

namespace Steverzag.PropertySelectors
{
	/// <summary>One overload of a function: what its arguments must be, what it returns, and how it builds its expression.</summary>
	/// <remarks>
	/// Overloads are matched by argument count, then by constraint. A function declares all of its overloads in one table,
	/// returned by <see cref="IFunctionSignatures.Signatures"/>. See "Extending: custom functions" in
	/// <c>docs/property-selectors.md</c>.
	/// </remarks>
	/// <param name="Parameters">The constraint for each argument, in order.</param>
	/// <param name="ResultType">
	/// Returns the type the function produces, given the target's type and the arguments' types. Either may be
	/// <see langword="null"/> when it isn't known yet; return <see langword="null"/> then rather than guess.
	/// </param>
	/// <param name="Bind">
	/// Receives the arguments and returns the function that builds the expression from the target. It runs when the
	/// overload is resolved, so it can validate the arguments, and an exception it throws is a parse error.
	/// </param>
	public sealed record FunctionSignature(
		IReadOnlyList<ArgumentConstraint> Parameters,
		Func<Type?, IReadOnlyList<Type?>, Type?> ResultType,
		Func<IReadOnlyList<PropertySelector>, Func<Expression?, Expression>> Bind)
	{
		/// <summary>
		/// What the piped value must be, for a function that works on it, such as
		/// <see cref="ArgumentConstraint.Collection"/> for an aggregate. <see cref="ArgumentConstraint.Any"/> by default.
		/// </summary>
		public ArgumentConstraint Target { get; init; } = ArgumentConstraint.Any;

		/// <summary>
		/// The constraint for any number of extra arguments after <see cref="Parameters"/>, or <see langword="null"/> if the
		/// overload takes exactly <see cref="Parameters"/>.
		/// </summary>
		public ArgumentConstraint? Rest { get; init; }

		/// <summary>Whether the overload can be called with a number of arguments.</summary>
		/// <param name="argumentCount">The number of arguments.</param>
		/// <returns><see langword="true"/> if the count matches, or is at least <see cref="Parameters"/>' count when there is a <see cref="Rest"/>.</returns>
		public bool Accepts(int argumentCount) => Rest is null ? argumentCount == Parameters.Count : argumentCount >= Parameters.Count;

		/// <summary>The constraint for the argument at an index: one of <see cref="Parameters"/>, or <see cref="Rest"/> after them.</summary>
		/// <param name="index">The 0-based argument index. It must be one <see cref="Accepts"/> allows.</param>
		/// <returns>The constraint.</returns>
		public ArgumentConstraint ConstraintAt(int index) => index < Parameters.Count ? Parameters[index] : Rest!;

		/// <summary>Returns the parameter list as text, such as <c>(a number, an integer)</c>.</summary>
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
